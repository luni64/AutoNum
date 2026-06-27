using AutoNumber.ViewModels;
using System.Drawing;

namespace AutoNumber.Model;

internal static class SizingModel
{
    public const double DefaultScale = 1.0;
    public const double LegacyPreviewFontFactor = 0.711;

    public static bool UseFaceBasedBaseLabelDiameter { get; set; } = false;

    public static double ComputeBaseLabelDiameter(IEnumerable<Rectangle> faces, int imageWidth)
    {
        var fallbackDiameter = Math.Max(1, imageWidth / 20.0);

        if (!UseFaceBasedBaseLabelDiameter)
        {
            return fallbackDiameter;
        }

        var faceList = faces?.ToList() ?? [];
        if (faceList.Count > 0)
        {
            return Math.Max(faceList.Average(m => m.Width), faceList.Average(m => m.Height)) / 2.0;
        }

        return fallbackDiameter;
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
