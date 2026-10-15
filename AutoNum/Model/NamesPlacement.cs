using System.Text.Json.Serialization;

namespace AutoNumber.Model;

/// <summary>
/// Where the names table sits relative to the photo in the preview and the exported image.
/// Serialised by name so settings.json and the embedded metadata stay readable.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NamesPlacement
{
    /// <summary>Full photo width, under the photo and the image-ID line (the only layout before 2.5).</summary>
    Below,

    /// <summary>A column to the right of the photo, top-aligned with it; the title spans photo and column.</summary>
    Right,
}

/// <summary>Range and default of the width of a names table beside the photo, as a fraction of the photo width.</summary>
public static class NamesSideWidth
{
    public const double Min = 0.15;
    public const double Max = 1.0;
    public const double Default = 0.5;

    public static double Clamp(double scale) => double.IsFinite(scale) ? Math.Clamp(scale, Min, Max) : Default;
}

/// <summary>
/// The names-table region in photo coordinates (photo top-left = 0,0; the title band above is
/// not included). <see cref="Beside"/> tells the renderers whether the composite grows to the
/// right (table beside the photo) or downward (table below it). Computed once by
/// <c>NameManager.ShowNames</c> and consumed by the WPF preview bindings and the JPG export.
/// </summary>
public sealed record NamesRegion(double X, double Y, double Width, double Height, bool Beside)
{
    public static readonly NamesRegion None = new(0, 0, 0, 0, false);
}
