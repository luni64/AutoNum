using AutoNumber.ViewModels;
using System.Drawing;

namespace AutoNumber.Model;

/// <summary>
/// How label and text sizes are derived for a fresh (or reopened) image. Two independent
/// 100% baselines feed everything else:
///
///   - Label diameter/font (the numbered circles) — derived from detected FACE size,
///     capped by image size for close-ups. See ComputeBaseLabelDiameter and
///     ComputeFittedLabelFontSize, and the "Label sizing" constants below.
///   - Text font (Namensliste/Title/Description/Image-ID) — derived from IMAGE size only,
///     independent of face detection or the label baseline. See ComputeBaseTextFontSize
///     and the "Text sizing" constant below.
///
/// Every displayed size is then baseline * a per-element FontScale/LabelScale
/// (0.25-4.0, user-adjustable), via ResolveSize.
/// </summary>
internal static class SizingModel
{
    /// <summary>
    /// "Unscaled" (100%) — the slider position new AppSettings/metadata default to before
    /// a settings.json or saved image has ever set an actual value, and the fallback
    /// SafeScale/ResolveSize use if a stored scale turns out to be invalid.
    /// </summary>
    public const double DefaultScale = 1.0;

    #region Tuning constants

    // --- Label sizing (face-based, see ComputeBaseLabelDiameter/ComputeFittedLabelFontSize) ---

    /// <summary>Label diameter = this fraction of the average detected face's diagonal.</summary>
    public const double LabelDiameterFaceFactor = 0.38;

    /// <summary>
    /// Upper bound on label diameter, as a fraction of the image's own diagonal. Keeps
    /// close-ups with a few large faces from producing oversized labels — the
    /// LabelDiameterFaceFactor formula alone is unbounded and just scales linearly with
    /// face size. Only bites when faces are large relative to the frame; group photos
    /// (small faces relative to the image) stay under this and are unaffected.
    /// </summary>
    public const double LabelDiameterImageCapFactor = 0.045;

    /// <summary>
    /// The label number's font size is fit so its rendered bounding-box diagonal comes out
    /// to this many times the label diameter. See ComputeFittedLabelFontSize.
    /// </summary>
    public const double LabelFontFitFactor = 1.7;

    // --- Text sizing (image-based, see ComputeBaseTextFontSize) ---

    /// <summary>
    /// Baseline for Namensliste/Title/Description/Image-ID font sizes: this fraction of
    /// the image's own diagonal, independent of face detection — avoids being skewed by
    /// how many/how large the detected faces happen to be, and isn't capped by the
    /// label-circle-fit constraint LabelFontFitFactor has. Only used for fresh images;
    /// reopened images restore the value in effect at save time
    /// (AutoNumMetaData_V3.BaseTextFontSize), so retuning this only affects new photos.
    /// </summary>
    public const double TextFontImageFactor = 0.023;

    // --- Legacy (pre-V3 metadata) ---

    private const double LegacyPreviewFontFactor = 0.711;

    #endregion

    public static double ComputeBaseLabelDiameter(IEnumerable<Rectangle> faces, int imageWidth, int imageHeight)
    {
        // Tends to run large, but is the only option when there's no face data at all
        // (detection disabled, or none found) — kept as-is for now.
        var fallbackDiameter = Math.Max(1, imageWidth / 20.0);

        var faceList = faces?.ToList() ?? [];
        if (faceList.Count == 0)
        {
            return fallbackDiameter;
        }

        // Diagonal is a single cheap-to-compute size measure per face (list is at most a
        // couple hundred entries, so no need to worry about the sqrt cost). No outlier
        // removal yet — add it here if testing against real photos shows it's needed.
        var averageFaceDiagonal = faceList.Average(f => Diagonal(f.Width, f.Height));
        var faceBasedDiameter = averageFaceDiagonal * LabelDiameterFaceFactor;
        var imageBasedCap = Diagonal(imageWidth, imageHeight) * LabelDiameterImageCapFactor;

        return Math.Max(1, Math.Min(faceBasedDiameter, imageBasedCap));
    }

    /// <summary>
    /// Reference size used to measure the label text before scaling it to the target size
    /// below — GDI+ has no "font size that produces this measurement" API, so this measures
    /// once at a fixed size and linearly extrapolates (font metrics scale proportionally
    /// with point size). Arbitrary otherwise; not a "12pt label" of any kind.
    /// </summary>
    private const float FontMeasurementReferenceSize = 12f;

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

        using var referenceFont = new Font(MarkerLabel.Style.FontFamily, FontMeasurementReferenceSize);
        var measuredDiagonalAtReferenceSize = Analyzer.GetCircumscribingDiameter(text, referenceFont);
        if (measuredDiagonalAtReferenceSize <= 0)
        {
            return 12;
        }

        // Scale the reference-size measurement up/down until it hits the target diagonal.
        var targetDiagonal = LabelFontFitFactor * diameter;
        return targetDiagonal / measuredDiagonalAtReferenceSize * FontMeasurementReferenceSize;
    }

    public static double ComputeBaseTextFontSize(int imageWidth, int imageHeight)
    {
        return Math.Max(1, Diagonal(imageWidth, imageHeight) * TextFontImageFactor);
    }

    private static double Diagonal(double width, double height) => Math.Sqrt(width * width + height * height);

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
