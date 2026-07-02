using QuestPDF.Fluent;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace AutoNumber.Model;

internal static class PdfPayloadStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public static byte[] CreatePayloadZip(PdfPayloadData payload)
    {
        var metadataJson = payload.Metadata.ToJson();
        var metadataBytes = Encoding.UTF8.GetBytes(metadataJson);

        var manifest = new PdfPayloadManifest
        {
            CreatedAt = DateTimeOffset.Now.ToString("O"),
            MetadataVersion = payload.Metadata.Version,
            MetadataSha256 = ComputeSha256(metadataBytes),
            BaseImageSha256 = ComputeSha256(payload.BaseImageBytes)
        };

        var manifestBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifest, JsonOptions));

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            AddZipEntry(zip, PdfPayloadContract.ManifestEntry, manifestBytes);
            AddZipEntry(zip, PdfPayloadContract.MetadataEntry, metadataBytes);
            AddZipEntry(zip, PdfPayloadContract.BaseImageEntry, payload.BaseImageBytes);
        }

        return ms.ToArray();
    }

    public static bool TryReadPayloadZip(byte[] payloadZipBytes, out PdfPayloadData? payload)
    {
        payload = null;

        try
        {
            using var ms = new MemoryStream(payloadZipBytes);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read, true);

            var manifestBytes = ReadZipEntry(zip, PdfPayloadContract.ManifestEntry);
            var metadataBytes = ReadZipEntry(zip, PdfPayloadContract.MetadataEntry);
            var baseImageBytes = ReadZipEntry(zip, PdfPayloadContract.BaseImageEntry);

            var manifest = JsonSerializer.Deserialize<PdfPayloadManifest>(manifestBytes, JsonOptions);
            if (manifest is null || manifest.Schema != "autonum-pdf-payload-v1")
            {
                return false;
            }

            if (!string.Equals(manifest.MetadataSha256, ComputeSha256(metadataBytes), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(manifest.BaseImageSha256, ComputeSha256(baseImageBytes), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var metadataJson = Encoding.UTF8.GetString(metadataBytes);
            if (!AutoNumMetaData_V1.FromJson(metadataJson, out var metadata) || metadata is null)
            {
                return false;
            }

            payload = new PdfPayloadData
            {
                Metadata = metadata,
                BaseImageBytes = baseImageBytes
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void SavePdfWithPayloadAttachment(byte[] pdfBytes, byte[] payloadZipBytes, string outputPdfPath)
    {
        var tempPdfPath = Path.Combine(Path.GetTempPath(), $"autonum-pdf-{Guid.NewGuid():N}.pdf");
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"autonum-payload-{Guid.NewGuid():N}.zip");

        try
        {
            File.WriteAllBytes(tempPdfPath, pdfBytes);
            File.WriteAllBytes(tempZipPath, payloadZipBytes);

            DocumentOperation
                .LoadFile(tempPdfPath)
                .AddAttachment(new DocumentOperation.DocumentAttachment
                {
                    Key = PdfPayloadContract.PayloadAttachmentKey,
                    FilePath = tempZipPath,
                    AttachmentName = PdfPayloadContract.PayloadAttachmentName,
                    MimeType = PdfPayloadContract.PayloadAttachmentMimeType,
                    Description = "Interne AutoNum-Bearbeitungsdaten",
                    Relationship = DocumentOperation.DocumentAttachmentRelationship.Unspecified,
                    CreationDate = DateTime.UtcNow,
                    ModificationDate = DateTime.UtcNow,
                    Replace = true
                })
                .Save(outputPdfPath);
        }
        finally
        {
            TryDeleteFile(tempPdfPath);
            TryDeleteFile(tempZipPath);
        }
    }

    public static bool TryExtractPayloadFromPdfAttachment(string pdfPath, out byte[]? payloadZipBytes)
    {
        payloadZipBytes = null;

        try
        {
            using var document = PdfDocument.Open(pdfPath);
            if (!document.Advanced.TryGetEmbeddedFiles(out var embeddedFiles) || embeddedFiles.Count == 0)
            {
                return false;
            }

            foreach (var embeddedFile in embeddedFiles)
            {
                if (string.Equals(embeddedFile.Name, PdfPayloadContract.PayloadAttachmentName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(embeddedFile.Name, PdfPayloadContract.PayloadAttachmentKey, StringComparison.OrdinalIgnoreCase))
                {
                    payloadZipBytes = embeddedFile.Bytes.ToArray();
                    return payloadZipBytes.Length > 0;
                }
            }

            foreach (var embeddedFile in embeddedFiles)
            {
                if (embeddedFile.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    payloadZipBytes = embeddedFile.Bytes.ToArray();
                    return payloadZipBytes.Length > 0;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best effort cleanup only
        }
    }

    private static void AddZipEntry(ZipArchive zip, string entryName, byte[] data)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.SmallestSize);
        using var stream = entry.Open();
        stream.Write(data, 0, data.Length);
    }

    private static byte[] ReadZipEntry(ZipArchive zip, string entryName)
    {
        var entry = zip.GetEntry(entryName) ?? throw new InvalidDataException($"Missing payload entry '{entryName}'.");
        using var stream = entry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static string ComputeSha256(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash);
    }
}
