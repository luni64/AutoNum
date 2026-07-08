using System.Text.Json.Serialization;

namespace AutoNumber.Model;

/// <summary>
/// Versioned AutoNum payload contract stored in PDF as a non-rendered embedded zip payload.
/// Payload contains metadata plus the editable base image.
/// </summary>
internal static class PdfPayloadContract
{
    public const string PayloadAttachmentKey = "autonum-data";
    public const string PayloadAttachmentName = "autonum-payload.zip";
    public const string PayloadAttachmentMimeType = "application/zip";

    public const string ManifestEntry = "autonum/manifest.json";
    public const string MetadataEntry = "autonum/metadata.json";
    public const string BaseImageEntry = "autonum/base.jpg";
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

    [JsonPropertyName("baseImageSha256")]
    public string BaseImageSha256 { get; set; } = string.Empty;
}

internal sealed class PdfPayloadData
{
    public required AutoNumMetaData_V1 Metadata { get; init; }

    public required byte[] BaseImageBytes { get; init; }
}
