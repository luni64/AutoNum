namespace AutoNumber.Model;

/// <summary>
/// Where a freshly detected face's label should be centered, relative to the
/// detected face rectangle. Applies only to new detections (open/redetect/rotate);
/// existing labels are never moved retroactively when this setting changes.
/// </summary>
public enum FaceLabelAnchor
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
}

public static class FaceLabelAnchorExtensions
{
    /// <summary>
    /// Fractional position within the face rectangle (0..1 on each axis) the label
    /// should be centered on.
    /// </summary>
    public static (double FracX, double FracY) ToFraction(this FaceLabelAnchor anchor) => anchor switch
    {
        FaceLabelAnchor.TopLeft => (0.0, 0.0),
        FaceLabelAnchor.TopCenter => (0.5, 0.0),
        FaceLabelAnchor.TopRight => (1.0, 0.0),
        FaceLabelAnchor.MiddleLeft => (0.0, 0.5),
        FaceLabelAnchor.Center => (0.5, 0.5),
        FaceLabelAnchor.MiddleRight => (1.0, 0.5),
        FaceLabelAnchor.BottomLeft => (0.0, 1.0),
        FaceLabelAnchor.BottomCenter => (0.5, 1.0),
        FaceLabelAnchor.BottomRight => (1.0, 1.0),
        _ => (0.5, 1.0),
    };
}
