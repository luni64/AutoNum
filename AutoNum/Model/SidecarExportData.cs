using System.Text.Json.Serialization;

namespace AutoNumber.Model;

/// <summary>
/// Person/title/description data exported alongside a save — as CSV/JSON sidecar files and as
/// the content of the printable PDF report (see <see cref="PdfReportRenderer"/>).
/// </summary>
public sealed class SidecarExportData
{
    [JsonPropertyName("generatedAt")]
    public string GeneratedAt { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("persons")]
    public List<SidecarPerson> Persons { get; set; } = [];
}

public sealed class SidecarPerson
{
    [JsonPropertyName("row")]
    public int Row { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
