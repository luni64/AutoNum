using AutoNumber.Infrastructure;
using System.IO;
using System.Text;

namespace AutoNumber.Model;

/// <summary>
/// Reads and writes AutoNum data stored in custom JPEG APP4 (0xFFE4) segments.
/// Two payload kinds share the same segment format and are told apart by their 8-byte magic:
/// <list type="bullet">
/// <item><c>"ANumJSON"</c> — the <see cref="AutoNumMetaData_V1"/> JSON (UTF-8).</item>
/// <item><c>"AutoNum\0"</c> — the pixel patches hidden under the labels.</item>
/// </list>
/// Each segment starts with the magic followed by segment-index/total-segments; a payload
/// larger than the ~64 KB per-segment limit is chained across several segments.
/// The JSON lives here rather than in the EXIF UserComment because the whole EXIF block
/// (thumbnail and MakerNote included) must fit into one 64 KB APP1 segment, and because
/// other tools (e.g. digiKam) treat UserComment as their own caption field and overwrite it.
/// </summary>
internal static class AppSegmentIO
{
    private static readonly byte[] PatchMagic = "AutoNum\0"u8.ToArray();
    private static readonly byte[] MetadataMagic = "ANumJSON"u8.ToArray();
    private const int MagicLength = 8;
    private const byte MarkerPrefix = 0xFF;
    private const byte App4Marker = 0xE4;
    private const int MaxSegmentPayload = 65533;       // max data bytes per APPn segment (excluding marker)
    private const int HeaderSize = MagicLength + 2 + 2; // magic + segmentIndex(2) + totalSegments(2)

    /// <summary>
    /// Serialises patch data into a single byte buffer using the binary format
    /// described in PATCH_RESTORE_PLAN.md.
    /// </summary>
    internal static byte[] SerialisePatches(List<PatchData> patches)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write((ushort)patches.Count);
        foreach (var p in patches)
        {
            bw.Write(p.X);
            bw.Write(p.Y);
            bw.Write(p.Width);
            bw.Write(p.Height);
            bw.Write((uint)p.PngBytes.Length);
            bw.Write(p.PngBytes);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Deserialises patch data from the binary format.
    /// </summary>
    internal static List<PatchData> DeserialisePatches(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);

        int count = br.ReadUInt16();
        var patches = new List<PatchData>(count);
        for (int i = 0; i < count; i++)
        {
            float x = br.ReadSingle();
            float y = br.ReadSingle();
            float w = br.ReadSingle();
            float h = br.ReadSingle();
            uint pngLen = br.ReadUInt32();
            byte[] png = br.ReadBytes((int)pngLen);
            patches.Add(new PatchData(x, y, w, h, png));
        }

        return patches;
    }

    /// <summary>
    /// Injects the AutoNum APP4 segments (metadata JSON first, then the patches) into a JPEG
    /// byte stream, right after the SOI marker. Returns a new byte array with the segments inserted.
    /// </summary>
    public static byte[] InjectSegments(byte[] jpegBytes, string metadataJson, List<PatchData> patches)
    {
        var segments = BuildSegments(MetadataMagic, Encoding.UTF8.GetBytes(metadataJson));
        if (patches.Count > 0)
            segments.AddRange(BuildSegments(PatchMagic, SerialisePatches(patches)));

        // JPEG starts with SOI (FF D8). Insert our segments right after SOI.
        using var output = new MemoryStream(jpegBytes.Length + segments.Sum(s => s.Length));
        output.Write(jpegBytes, 0, 2); // SOI

        foreach (var seg in segments)
            output.Write(seg);

        output.Write(jpegBytes, 2, jpegBytes.Length - 2); // rest of JPEG
        return output.ToArray();
    }

    /// <summary>
    /// Reads the AutoNum metadata JSON from the APP4 segments of a JPEG byte stream.
    /// Returns null if the JPEG carries no such segment (plain photo, or a file written by
    /// an AutoNum version that still stored the JSON in the EXIF UserComment).
    /// </summary>
    public static string? ReadMetadataJson(byte[] jpegBytes)
    {
        var payload = ReadPayload(jpegBytes, MetadataMagic);
        return payload is null ? null : Encoding.UTF8.GetString(payload);
    }

    /// <summary>
    /// Reads the AutoNum patch segments from a JPEG byte stream and returns the patch list.
    /// Returns null if no patch segments are found or they cannot be decoded.
    /// </summary>
    public static List<PatchData>? ReadPatches(byte[] jpegBytes)
    {
        var payload = ReadPayload(jpegBytes, PatchMagic);
        if (payload is null) return null;

        try
        {
            return DeserialisePatches(payload);
        }
        catch (Exception ex)
        {
            AppLog.Warn("Open: embedded patch segments present but not decodable", ex);
            return null;
        }
    }

    /// <summary>
    /// Reassembles the (possibly chained) payload of all APP4 segments carrying the given magic.
    /// </summary>
    private static byte[]? ReadPayload(byte[] jpegBytes, byte[] magic)
    {
        var rawSegments = ExtractRawSegments(jpegBytes, magic);
        if (rawSegments.Count == 0) return null;

        rawSegments.Sort((a, b) => a.Index.CompareTo(b.Index));

        using var ms = new MemoryStream();
        foreach (var seg in rawSegments)
            ms.Write(seg.Data);

        return ms.ToArray();
    }

    /// <summary>
    /// Builds one or more raw APP4 segment byte arrays from a payload, chaining if needed.
    /// </summary>
    private static List<byte[]> BuildSegments(byte[] magic, byte[] payload)
    {
        int maxDataPerSegment = MaxSegmentPayload - 2 - HeaderSize; // -2 for length field itself
        int totalSegments = (payload.Length + maxDataPerSegment - 1) / maxDataPerSegment;
        if (totalSegments == 0) totalSegments = 1;

        var segments = new List<byte[]>(totalSegments);

        int offset = 0;
        for (int i = 0; i < totalSegments; i++)
        {
            int chunkSize = Math.Min(maxDataPerSegment, payload.Length - offset);

            using var ms = new MemoryStream();
            // APP4 marker
            ms.WriteByte(MarkerPrefix);
            ms.WriteByte(App4Marker);

            // Segment length (big-endian): includes the 2 length bytes + header + data
            int segmentLength = 2 + HeaderSize + chunkSize;
            ms.WriteByte((byte)(segmentLength >> 8));
            ms.WriteByte((byte)(segmentLength & 0xFF));

            // Header: magic + index + total
            ms.Write(magic);
            ms.WriteByte((byte)(i & 0xFF));
            ms.WriteByte((byte)((i >> 8) & 0xFF));
            ms.WriteByte((byte)(totalSegments & 0xFF));
            ms.WriteByte((byte)((totalSegments >> 8) & 0xFF));

            // Payload chunk
            ms.Write(payload, offset, chunkSize);
            offset += chunkSize;

            segments.Add(ms.ToArray());
        }

        return segments;
    }

    /// <summary>
    /// Scans a JPEG byte stream for APP4 segments with the given magic prefix.
    /// Returns the raw payload chunks with their segment indices.
    /// </summary>
    private static List<(int Index, int Total, byte[] Data)> ExtractRawSegments(byte[] jpeg, byte[] magic)
    {
        var results = new List<(int, int, byte[])>();
        int pos = 2; // skip SOI (FF D8)

        while (pos + 3 < jpeg.Length)
        {
            if (jpeg[pos] != MarkerPrefix)
            {
                pos++;
                continue;
            }

            byte markerType = jpeg[pos + 1];

            // End of headers — stop scanning
            if (markerType == 0xDA) // SOS — start of scan, no more metadata segments
                break;

            int segLen = (jpeg[pos + 2] << 8) | jpeg[pos + 3]; // big-endian length

            if (markerType == App4Marker)
            {
                int dataStart = pos + 4; // after marker(2) + length(2)
                int dataLen = segLen - 2; // length includes its own 2 bytes

                if (dataLen >= HeaderSize
                    && dataStart + dataLen <= jpeg.Length
                    && jpeg.AsSpan(dataStart, MagicLength).SequenceEqual(magic))
                {
                    int headerOffset = dataStart + MagicLength;
                    int segIndex = jpeg[headerOffset] | (jpeg[headerOffset + 1] << 8);
                    int segTotal = jpeg[headerOffset + 2] | (jpeg[headerOffset + 3] << 8);

                    int payloadStart = dataStart + HeaderSize;
                    int payloadLen = dataLen - HeaderSize;

                    var chunk = new byte[payloadLen];
                    Buffer.BlockCopy(jpeg, payloadStart, chunk, 0, payloadLen);
                    results.Add((segIndex, segTotal, chunk));
                }
            }

            pos += 2 + segLen; // skip marker(2) + segLen
        }

        return results;
    }
}
