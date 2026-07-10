using AutoNumber.ViewModels;
using System.Drawing;

namespace AutoNumber.Model;

internal static class SizingModel
{
    public const double DefaultScale = 1.0;
    public const double LegacyPreviewFontFactor = 0.711;

    /// <summary>
    /// Dev-only switch for comparing face-based vs. image-width-based base label sizing
    /// while retuning the former against the YuNet detector; not exposed in Settings.
    /// </summary>
    public static bool UseFaceBasedBaseLabelDiameter { get; set; } = true;

    /// <summary>
    /// Fraction of the average detected face's diagonal used as the base label diameter.
    /// </summary>
    public const double FaceDiagonalFactor = 0.38;

    /// <summary>
    /// Upper bound on the base label diameter, as a fraction of the image's own diagonal.
    /// Keeps close-ups with a few large faces from producing oversized labels — the
    /// FaceDiagonalFactor formula alone is unbounded and just scales linearly with face size.
    /// Only bites when faces are large relative to the frame; group photos (small faces
    /// relative to the image) stay under this and are unaffected.
    /// </summary>
    public const double ImageDiagonalCapFactor = 0.045;

    public static double ComputeBaseLabelDiameter(IEnumerable<Rectangle> faces, int imageWidth, int imageHeight)
    {
        // Tends to run large, but is the only option when there's no face data at all
        // (detection disabled, or none found) — kept as-is for now.
        var fallbackDiameter = Math.Max(1, imageWidth / 20.0);

        if (!UseFaceBasedBaseLabelDiameter)
        {
            return fallbackDiameter;
        }

        var faceList = faces?.ToList() ?? [];
        if (faceList.Count == 0)
        {
            return fallbackDiameter;
        }

        // Diagonal is a single cheap-to-compute size measure per face (list is at most a
        // couple hundred entries, so no need to worry about the sqrt cost). No outlier
        // removal yet — add it here if testing against real photos shows it's needed.
        var averageDiagonal = faceList.Average(f => Math.Sqrt((double)f.Width * f.Width + (double)f.Height * f.Height));
        var faceBasedDiameter = averageDiagonal * FaceDiagonalFactor;

        var imageDiagonal = Math.Sqrt((double)imageWidth * imageWidth + (double)imageHeight * imageHeight);
        var imageBasedCap = imageDiagonal * ImageDiagonalCapFactor;

        return Math.Max(1, Math.Min(faceBasedDiameter, imageBasedCap));
    }

    public static double ComputeFittedLabelFontSize(double diameter, IEnumerable<Person> persons)
    {
        if (!double.IsFinite(diameter) || diameter <= 0)
        {
            return 12;
        }

        var text = persons
            .Select(p => p.Label.Number)
            .DefaultIfEmpty(1)
            .Max()
            .ToString();

        using var font = new Font(MarkerLabel.Style.FontFamily, 12f);
        var measured = Analyzer.GetCircumscribingDiameter(text, font);
        if (measured <= 0)
        {
            return 12;
        }

        return 1.5 * diameter / measured * 12.0;
    }

    public static double SafeScale(double actualSize, double baseSize)
    {
        if (!double.IsFinite(actualSize) || !double.IsFinite(baseSize) || actualSize <= 0 || baseSize <= 0)
        {
            return DefaultScale;
        }

        return actualSize / baseSize;
    }

    public static double ResolveSize(double baseSize, double scale)
    {
        if (!double.IsFinite(baseSize) || baseSize <= 0)
        {
            return 0;
        }

        return baseSize * (double.IsFinite(scale) && scale > 0 ? scale : DefaultScale);
    }

    public static double LegacyStoredFontSizeToVisibleSize(double storedFontSize)
    {
        if (!double.IsFinite(storedFontSize) || storedFontSize <= 0)
        {
            return 12;
        }

        return storedFontSize * LegacyPreviewFontFactor;
    }
}
