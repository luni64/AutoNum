using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;

namespace AutoNumber.Model;

/// <summary>
/// Reads and writes AutoNum patch data stored in custom JPEG APP4 (0xFFE4) segments.
/// Each segment starts with the magic prefix "AutoNum\0" followed by segment/chain metadata.
/// Multiple segments are chained when the payload exceeds the ~64 KB per-segment limit.
/// </summary>
internal static class AppSegmentIO
{
    private static readonly byte[] Magic = "AutoNum\0"u8.ToArray();
    private const byte MarkerPrefix = 0xFF;
    private const byte App4Marker = 0xE4;
    private const int MaxSegmentPayload = 65533; // max data bytes per APPn segment (excluding marker)
    private const int HeaderSize = 8 + 2 + 2;    // magic(8) + segmentIndex(2) + totalSegments(2)

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
    /// Injects AutoNum APP4 segment(s) into a JPEG byte stream, right after the SOI marker.
    /// Returns a new byte array with the segments inserted.
    /// </summary>
    public static byte[] InjectSegments(byte[] jpegBytes, List<PatchData> patches)
    {
        if (patches.Count == 0) return jpegBytes;

        byte[] payload = SerialisePatches(patches);
        var segments = BuildSegments(payload);

        // JPEG starts with SOI (FF D8). Insert our segments right after SOI.
        using var output = new MemoryStream(jpegBytes.Length + segments.Sum(s => s.Length));
        output.Write(jpegBytes, 0, 2); // SOI

        foreach (var seg in segments)
            output.Write(seg);

        output.Write(jpegBytes, 2, jpegBytes.Length - 2); // rest of JPEG
        return output.ToArray();
    }

    /// <summary>
    /// Reads AutoNum APP4 segments from a JPEG byte stream and returns the patch list.
    /// Returns null if no AutoNum segments are found.
    /// </summary>
    public static List<PatchData>? ReadSegments(byte[] jpegBytes)
    {
        var rawSegments = ExtractRawSegments(jpegBytes);
        if (rawSegments.Count == 0) return null;

        // Sort by segment index and reassemble
        rawSegments.Sort((a, b) => a.Index.CompareTo(b.Index));

        using var ms = new MemoryStream();
        foreach (var seg in rawSegments)
            ms.Write(seg.Data);

        try
        {
            return DeserialisePatches(ms.ToArray());
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to deserialise AutoNum patches: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Quick check whether the JPEG contains any AutoNum APP4 segments.
    /// </summary>
    public static bool HasSegments(byte[] jpegBytes)
    {
        return ExtractRawSegments(jpegBytes).Count > 0;
    }

    /// <summary>
    /// Builds one or more raw APP4 segment byte arrays from a payload, chaining if needed.
    /// </summary>
    private static List<byte[]> BuildSegments(byte[] payload)
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
            ms.Write(Magic);
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
    /// Scans a JPEG byte stream for APP4 segments with the AutoNum magic prefix.
    /// Returns the raw payload chunks with their segment indices.
    /// </summary>
    private static List<(int Index, int Total, byte[] Data)> ExtractRawSegments(byte[] jpeg)
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

            if (markerType == App4Marker)
            {
                int segLen = (jpeg[pos + 2] << 8) | jpeg[pos + 3]; // big-endian length
                int dataStart = pos + 4; // after marker(2) + length(2)
                int dataLen = segLen - 2; // length includes its own 2 bytes

                if (dataLen >= HeaderSize && dataStart + dataLen <= jpeg.Length)
                {
                    // Check magic prefix
                    bool isMagicMatch = true;
                    for (int m = 0; m < Magic.Length; m++)
                    {
                        if (jpeg[dataStart + m] != Magic[m])
                        {
                            isMagicMatch = false;
                            break;
                        }
                    }

                    if (isMagicMatch)
                    {
                        int headerOffset = dataStart + Magic.Length;
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
            else
            {
                // Skip other marker segments
                if (pos + 3 < jpeg.Length)
                {
                    int segLen = (jpeg[pos + 2] << 8) | jpeg[pos + 3];
                    pos += 2 + segLen;
                }
                else
                {
                    break;
                }
            }
        }

        return results;
    }
}
