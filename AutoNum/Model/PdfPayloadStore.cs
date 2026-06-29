using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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

        var patchEntries = payload.Patches
            .Select(p => new PdfPatchEntry
            {
                X = p.X,
                Y = p.Y,
                Width = p.Width,
                Height = p.Height,
                PngBytes = p.PngBytes
            })
            .ToList();

        var patchesBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(patchEntries, JsonOptions));

        var manifest = new PdfPayloadManifest
        {
            CreatedAt = DateTimeOffset.Now.ToString("O"),
            MetadataVersion = payload.Metadata.Version,
            MetadataSha256 = ComputeSha256(metadataBytes),
            CompositeSha256 = ComputeSha256(payload.CompositeImageBytes),
            PatchesSha256 = ComputeSha256(patchesBytes)
        };

        var manifestBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifest, JsonOptions));

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            AddZipEntry(zip, PdfPayloadContract.ManifestEntry, manifestBytes);
            AddZipEntry(zip, PdfPayloadContract.MetadataEntry, metadataBytes);
            AddZipEntry(zip, PdfPayloadContract.CompositeImageEntry, payload.CompositeImageBytes);
            AddZipEntry(zip, PdfPayloadContract.PatchesEntry, patchesBytes);
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
            var compositeBytes = ReadZipEntry(zip, PdfPayloadContract.CompositeImageEntry);
            var patchesBytes = ReadZipEntry(zip, PdfPayloadContract.PatchesEntry);

            var manifest = JsonSerializer.Deserialize<PdfPayloadManifest>(manifestBytes, JsonOptions);
            if (manifest is null || manifest.Schema != "autonum-pdf-payload-v1")
            {
                return false;
            }

            if (!string.Equals(manifest.MetadataSha256, ComputeSha256(metadataBytes), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(manifest.CompositeSha256, ComputeSha256(compositeBytes), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(manifest.PatchesSha256, ComputeSha256(patchesBytes), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var metadataJson = Encoding.UTF8.GetString(metadataBytes);
            if (!AutoNumMetaData_V1.FromJson(metadataJson, out var metadata) || metadata is null)
            {
                return false;
            }

            var patchEntries = JsonSerializer.Deserialize<List<PdfPatchEntry>>(patchesBytes, JsonOptions) ?? [];
            var patches = patchEntries
                .Select(p => new PatchData(p.X, p.Y, p.Width, p.Height, p.PngBytes))
                .ToList();

            payload = new PdfPayloadData
            {
                Metadata = metadata,
                CompositeImageBytes = compositeBytes,
                Patches = patches
            };

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static byte[] EmbedPayload(byte[] pdfBytes, byte[] payloadZipBytes)
    {
        var magicBytes = Encoding.UTF8.GetBytes(PdfPayloadContract.FooterMagic);
        var magicLengthBytes = BitConverter.GetBytes(magicBytes.Length);
        var versionBytes = BitConverter.GetBytes(PdfPayloadContract.FooterVersion);
        var payloadLengthBytes = BitConverter.GetBytes((long)payloadZipBytes.Length);

        using var ms = new MemoryStream(pdfBytes.Length + payloadZipBytes.Length + magicBytes.Length + 4 + 4 + 8);
        ms.Write(pdfBytes, 0, pdfBytes.Length);
        ms.Write(payloadZipBytes, 0, payloadZipBytes.Length);
        ms.Write(magicBytes, 0, magicBytes.Length);
        ms.Write(versionBytes, 0, versionBytes.Length);
        ms.Write(payloadLengthBytes, 0, payloadLengthBytes.Length);
        ms.Write(magicLengthBytes, 0, magicLengthBytes.Length);

        return ms.ToArray();
    }

    public static bool TryExtractPayload(byte[] pdfBytes, out byte[]? payloadZipBytes)
    {
        payloadZipBytes = null;

        if (pdfBytes.Length < 4 + 8 + 4)
        {
            return false;
        }

        try
        {
            var magicLength = BitConverter.ToInt32(pdfBytes, pdfBytes.Length - 4);
            if (magicLength <= 0 || magicLength > 128)
            {
                return false;
            }

            var payloadLengthOffset = pdfBytes.Length - 4 - 8;
            var versionOffset = payloadLengthOffset - 4;
            var magicOffset = versionOffset - magicLength;

            if (magicOffset < 0)
            {
                return false;
            }

            var version = BitConverter.ToInt32(pdfBytes, versionOffset);
            if (version != PdfPayloadContract.FooterVersion)
            {
                return false;
            }

            var magic = Encoding.UTF8.GetString(pdfBytes, magicOffset, magicLength);
            if (!string.Equals(magic, PdfPayloadContract.FooterMagic, StringComparison.Ordinal))
            {
                return false;
            }

            var payloadLength = BitConverter.ToInt64(pdfBytes, payloadLengthOffset);
            if (payloadLength <= 0 || payloadLength > int.MaxValue)
            {
                return false;
            }

            var payloadOffset = magicOffset - payloadLength;
            if (payloadOffset < 0 || payloadOffset > int.MaxValue)
            {
                return false;
            }

            payloadZipBytes = new byte[(int)payloadLength];
            Buffer.BlockCopy(pdfBytes, (int)payloadOffset, payloadZipBytes, 0, (int)payloadLength);
            return true;
        }
        catch
        {
            return false;
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
