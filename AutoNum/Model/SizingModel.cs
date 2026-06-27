using AutoNumber.ViewModels;
using System.Drawing;

namespace AutoNumber.Model;

internal static class SizingModel
{
    public const double DefaultScale = 1.0;
    public const double SliderPercentMin = 0.0;
    public const double SliderPercentMax = 1.0;
    public const double SliderPercentDefault = 0.5;
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

    /// <summary>
    /// Converts slider position (0–1) to scale (0.25–4.0) using an exponential mapping.
    /// 
    /// VIEW → MODEL conversion. Exponential: scale = 0.25 * (16 ^ x)
    /// where x = slider position in [0, 1]
    /// 
    /// Maps:
    /// - slider 0.0 (left) → scale 0.25
    /// - slider 0.5 (center) → scale 1.0
    /// - slider 1.0 (right) → scale 4.0
    /// 
    /// This exponential mapping provides smooth, continuous coverage across the full
    /// slider range with no dead zones, allowing fine control throughout.
    /// </summary>
    public static double SliderToScale(double sliderPosition)
    {
        var clamped = Math.Clamp(sliderPosition, SliderPercentMin, SliderPercentMax);

        // Exponential mapping: scale = 0.25 * (16 ^ sliderPosition)
        // 16 is chosen so that 16^0.5 = 4, giving us scale 1.0 at slider 0.5
        var result = 0.25 * Math.Pow(16.0, clamped);
        return result;
    }

    /// <summary>
    /// Converts scale (0.25–4.0) back to slider position (0–1) using the inverse of the exponential mapping.
    /// 
    /// MODEL → VIEW conversion. Solves: scale = 0.25 * (16 ^ x) for x.
    /// Inverse: x = log₁₆(scale / 0.25) = log(scale / 0.25) / log(16)
    /// </summary>
    public static double ScaleToSlider(double scale)
    {
        if (!double.IsFinite(scale) || scale <= 0)
        {
            return SliderPercentDefault;
        }

        // Exponential inverse: x = log₁₆(scale / 0.25)
        // Using change of base: log₁₆(y) = log(y) / log(16)
        var ratio = scale / 0.25;
        var x = Math.Log(ratio) / Math.Log(16.0);

        // Clamp to valid slider range
        var clamped = Math.Clamp(x, SliderPercentMin, SliderPercentMax);
        return clamped;
    }

    // Backward compatibility aliases (will be removed after migration)
    [Obsolete("Use SliderToScale instead")]
    public static double SliderPercentToScale(double sliderPercent) => SliderToScale(sliderPercent);

    [Obsolete("Use ScaleToSlider instead")]
    public static double ScaleToSliderPercent(double scale) => ScaleToSlider(scale);

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
