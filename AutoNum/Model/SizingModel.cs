using AutoNumber.ViewModels;
using System.Drawing;

namespace AutoNumber.Model;

internal static class SizingModel
{
    public const double DefaultScale = 1.0;
    public const double SliderPercentMin = 25.0;
    public const double SliderPercentMax = 200.0;
    public const double SliderPercentDefault = 100.0;
    public const double LegacyPreviewFontFactor = 0.711;

    public static double ComputeBaseLabelDiameter(IEnumerable<Rectangle> faces, int imageWidth)
    {
        var faceList = faces?.ToList() ?? [];
        if (faceList.Count > 0)
        {
            return Math.Max(faceList.Average(m => m.Width), faceList.Average(m => m.Height)) / 2.0;
        }

        return Math.Max(1, imageWidth / 20.0);
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

    public static double SliderPercentToScale(double sliderPercent)
    {
        var clamped = Math.Clamp(sliderPercent, SliderPercentMin, SliderPercentMax);
        return clamped / 100.0;
    }

    public static double ScaleToSliderPercent(double scale)
    {
        if (!double.IsFinite(scale) || scale <= 0)
        {
            return SliderPercentDefault;
        }

        return Math.Clamp(scale * 100.0, SliderPercentMin, SliderPercentMax);
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
