using System.Text.Json.Serialization;

namespace AutoNumber.Model;

/// <summary>
/// Versioned AutoNum payload contract stored in PDF as a non-rendered embedded zip payload.
/// </summary>
internal static class PdfPayloadContract
{
    public const string FooterMagic = "AUTONUM_PDF_PAYLOAD_V1";
    public const int FooterVersion = 1;

    public const string ManifestEntry = "autonum/manifest.json";
    public const string MetadataEntry = "autonum/metadata.json";
    public const string CompositeImageEntry = "autonum/composite.jpg";
    public const string PatchesEntry = "autonum/patches.json";
}

internal sealed class PdfPayloadManifest
{
    [JsonPropertyName("schema")]
    public string Schema { get; set; } = "autonum-pdf-payload-v1";

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("metadataVersion")]
    public string MetadataVersion { get; set; } = string.Empty;

    [JsonPropertyName("metadataSha256")]
    public string MetadataSha256 { get; set; } = string.Empty;

    [JsonPropertyName("compositeSha256")]
    public string CompositeSha256 { get; set; } = string.Empty;

    [JsonPropertyName("patchesSha256")]
    public string PatchesSha256 { get; set; } = string.Empty;
}

internal sealed class PdfPatchEntry
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("width")]
    public float Width { get; set; }

    [JsonPropertyName("height")]
    public float Height { get; set; }

    [JsonPropertyName("png")]
    public byte[] PngBytes { get; set; } = [];
}

internal sealed class PdfPayloadData
{
    public required AutoNumMetaData_V1 Metadata { get; init; }

    public required byte[] CompositeImageBytes { get; init; }

    public required List<PatchData> Patches { get; init; }
}
