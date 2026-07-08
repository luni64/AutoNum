using QuestPDF.Fluent;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

            AppendPageModeUseNone(outputPdfPath);
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

    // -----------------------------------------------------------------------
    // PDF incremental update: set /PageMode /UseNone in the document catalog.
    // This prevents PDF viewers such as Acrobat from automatically opening the
    // attachments panel when the PDF contains embedded file attachments.
    //
    // Only traditional cross-reference tables (PDF 1.4 format, as produced by
    // QuestPDF/QPDF for this app) are handled.  If an xref stream is detected
    // the method returns silently without modifying the file.
    // -----------------------------------------------------------------------

    internal static void AppendPageModeUseNone(string pdfPath)
    {
        var bytes = File.ReadAllBytes(pdfPath);
        // Latin-1 maps every byte to the identical code-point, so byte offsets
        // and string indices are identical — essential for xref table math.
        var text = Encoding.Latin1.GetString(bytes);

        // Find the last startxref value (points to xref table or xref stream).
        var startxrefMatch = Regex.Match(text, @"startxref\s+(\d+)\s+%%EOF", RegexOptions.RightToLeft);
        if (!startxrefMatch.Success) return;
        long prevStartxref = long.Parse(startxrefMatch.Groups[1].Value);

        int xrefPos = (int)prevStartxref;
        if (xrefPos < 0 || xrefPos >= text.Length) return;

        // Detect whether this is a traditional xref table.
        // An xref stream would start with "N G obj" instead of "xref".
        int checkPos = xrefPos;
        while (checkPos < text.Length && text[checkPos] is ' ' or '\r' or '\n') checkPos++;
        if (checkPos + 4 > text.Length || text[checkPos..].AsSpan()[..4] is not "xref") return;

        // Find the last trailer dictionary.
        int trailerIdx = text.LastIndexOf("\ntrailer", StringComparison.Ordinal);
        if (trailerIdx < 0) return;
        trailerIdx++; // skip the leading \n

        int trailerDictStart = text.IndexOf("<<", trailerIdx, StringComparison.Ordinal);
        if (trailerDictStart < 0) return;

        int trailerDictEnd = PdfDictEnd(text, trailerDictStart);
        if (trailerDictEnd < 0) return;

        var trailerDictText = text.Substring(trailerDictStart, trailerDictEnd - trailerDictStart + 2);

        // Extract /Root (catalog object reference) and /Size.
        var rootRefMatch = Regex.Match(trailerDictText, @"/Root\s+(\d+)\s+(\d+)\s+R");
        if (!rootRefMatch.Success) return;
        int catalogObjNum = int.Parse(rootRefMatch.Groups[1].Value);
        int catalogGenNum = int.Parse(rootRefMatch.Groups[2].Value);

        var sizeMatch = Regex.Match(trailerDictText, @"/Size\s+(\d+)");
        if (!sizeMatch.Success) return;

        // Locate the catalog object byte offset in the xref table.
        long catalogByteOffset = PdfXrefEntry(text, xrefPos, catalogObjNum);
        if (catalogByteOffset < 0 || catalogByteOffset >= text.Length) return;

        // Parse the "N G obj" header at that offset, then find the dict.
        var objHeaderPat = new Regex($@"{catalogObjNum}\s+{catalogGenNum}\s+obj\s*");
        var objHeaderMatch = objHeaderPat.Match(text, (int)catalogByteOffset);
        if (!objHeaderMatch.Success || objHeaderMatch.Index - (int)catalogByteOffset > 50) return;

        int catalogDictStart = objHeaderMatch.Index + objHeaderMatch.Length;
        while (catalogDictStart < text.Length && text[catalogDictStart] is ' ' or '\t' or '\r' or '\n')
            catalogDictStart++;

        if (catalogDictStart + 1 >= text.Length || text[catalogDictStart] != '<' || text[catalogDictStart + 1] != '<')
            return;

        int catalogDictEnd = PdfDictEnd(text, catalogDictStart);
        if (catalogDictEnd < 0) return;

        var catalogDictContent = text.Substring(catalogDictStart, catalogDictEnd - catalogDictStart + 2);

        // Nothing to do if /PageMode /UseNone is already set.
        if (Regex.IsMatch(catalogDictContent, @"/PageMode\s*/UseNone\b")) return;

        // Add or replace /PageMode.
        string updatedCatalogDict;
        if (Regex.IsMatch(catalogDictContent, @"/PageMode\s*/\w+"))
        {
            updatedCatalogDict = Regex.Replace(catalogDictContent, @"/PageMode\s*/\w+", "/PageMode /UseNone");
        }
        else
        {
            // Insert /PageMode /UseNone just before the catalog dict's closing >>.
            updatedCatalogDict = catalogDictContent.TrimEnd()[..^2] + "\r\n/PageMode /UseNone\r\n>>";
        }

        // Build the replacement catalog object.
        // Use \r\n throughout so Acrobat DC's strict xref parser accepts the incremental update.
        var newObjContent = $"{catalogObjNum} {catalogGenNum} obj\r\n{updatedCatalogDict}\r\nendobj\r\n";
        var newObjBytes = Encoding.Latin1.GetBytes(newObjContent);
        long newObjOffset = bytes.Length;

        // xref entry: exactly 20 bytes — "NNNNNNNNNN GGGGG n \r\n" (no space before \r\n)
        var xrefEntry = FormattableString.Invariant($"{newObjOffset:D10} {catalogGenNum:D5} n\r\n");
        System.Diagnostics.Debug.Assert(Encoding.Latin1.GetByteCount(xrefEntry) == 20);
        // Xref keyword and subsection header must also use \r\n for consistent line endings.
        var newXrefContent = $"xref\r\n{catalogObjNum} 1\r\n{xrefEntry}";
        var newXrefBytes = Encoding.Latin1.GetBytes(newXrefContent);
        long newXrefOffset = newObjOffset + newObjBytes.Length;

        // New trailer: preserve all entries, strip any existing /Prev, add new /Prev.
        // Trim trailing whitespace robustly before removing the closing >> to handle any
        // line-ending variant in the original trailer dict.
        var updatedTrailerDict = Regex.Replace(trailerDictText, @"\s*/Prev\s+\d+", "");
        updatedTrailerDict = updatedTrailerDict.TrimEnd();
        updatedTrailerDict = updatedTrailerDict[..^2] + $"\r\n/Prev {prevStartxref}\r\n>>";
        var appendContent = $"trailer\r\n{updatedTrailerDict}\r\nstartxref\r\n{newXrefOffset}\r\n%%EOF\r\n";
        var appendBytes = Encoding.Latin1.GetBytes(appendContent);

        // Append the incremental update to the file.
        using var fs = File.Open(pdfPath, FileMode.Append, FileAccess.Write);
        fs.Write(newObjBytes, 0, newObjBytes.Length);
        fs.Write(newXrefBytes, 0, newXrefBytes.Length);
        fs.Write(appendBytes, 0, appendBytes.Length);
    }

    /// <summary>
    /// Returns the index of the closing <c>&gt;&gt;</c> that matches the opening
    /// <c>&lt;&lt;</c> at <paramref name="dictStart"/>.
    /// Handles nested dicts, PDF literal strings <c>(...)</c>, hex strings
    /// <c>&lt;...&gt;</c>, and line comments.
    /// Returns -1 if no matching close is found.
    /// </summary>
    private static int PdfDictEnd(string text, int dictStart)
    {
        int depth = 0;
        int i = dictStart;
        while (i < text.Length)
        {
            char c = text[i];
            switch (c)
            {
                case '(':
                    // Literal string — skip to matching ')'.
                    i++;
                    int parenDepth = 1;
                    while (i < text.Length && parenDepth > 0)
                    {
                        if (text[i] == '\\') { i += 2; continue; }
                        if (text[i] == '(') parenDepth++;
                        else if (text[i] == ')') parenDepth--;
                        i++;
                    }
                    break;
                case '<':
                    if (i + 1 < text.Length && text[i + 1] == '<')
                    {
                        depth++;
                        i += 2;
                    }
                    else
                    {
                        // Hex string <hexdigits> — skip to '>'.
                        int closeAngle = text.IndexOf('>', i + 1);
                        i = closeAngle >= 0 ? closeAngle + 1 : i + 1;
                    }
                    break;
                case '>':
                    if (i + 1 < text.Length && text[i + 1] == '>')
                    {
                        depth--;
                        if (depth == 0) return i;
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                    break;
                case '%':
                    // Line comment — skip to end of line.
                    while (i < text.Length && text[i] != '\n') i++;
                    break;
                default:
                    i++;
                    break;
            }
        }
        return -1;
    }

    /// <summary>
    /// Parses the traditional PDF cross-reference table starting at
    /// <paramref name="xrefTableStart"/> and returns the byte offset of
    /// <paramref name="targetObjNum"/>.  Returns -1 if not found or if the
    /// entry is free (<c>f</c>).
    /// </summary>
    private static long PdfXrefEntry(string text, int xrefTableStart, int targetObjNum)
    {
        // Skip the "xref" keyword and its trailing newline.
        int pos = text.IndexOf('\n', xrefTableStart);
        if (pos < 0) return -1;
        pos++;

        while (pos < text.Length)
        {
            while (pos < text.Length && text[pos] is ' ' or '\t') pos++;

            // Stop when we reach the trailer keyword.
            if (pos + 7 <= text.Length && text[pos..(pos + 7)] == "trailer") break;

            // Read subsection header: "firstObj count".
            int nlPos = text.IndexOf('\n', pos);
            if (nlPos < 0) break;

            var header = text[pos..nlPos].Trim();
            var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !int.TryParse(parts[0], out int first) || !int.TryParse(parts[1], out int count))
                break;

            pos = nlPos + 1;

            // Each xref entry is exactly 20 bytes.
            if (targetObjNum >= first && targetObjNum < first + count)
            {
                int entryPos = pos + (targetObjNum - first) * 20;
                if (entryPos + 20 <= text.Length)
                {
                    var entry = text[entryPos..(entryPos + 20)];
                    var ep = entry.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (ep.Length >= 3 && ep[2] == "n" && long.TryParse(ep[0], out long offset))
                        return offset;
                }
                return -1; // entry is free or malformed
            }

            pos += count * 20;
        }
        return -1;
    }
}
