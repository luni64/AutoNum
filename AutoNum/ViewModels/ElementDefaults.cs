using System.Drawing;

namespace AutoNumber.ViewModels;

/// <summary>
/// Formatting defaults for one visual element (number labels, title, description, image-ID,
/// names list), bound by the settings dialog. <see cref="EdgeColor"/> is only meaningful for
/// the number labels, <see cref="Enabled"/> (default visibility) only for the four text
/// elements — the unused property simply stays at its default for the others.
/// </summary>
public sealed class ElementDefaults : BaseViewModel
{
    /// <summary>Scale factor (0.25–4.0) applied to the element's computed base size.</summary>
    public double Scale
    {
        get => _scale;
        set => SetProperty(ref _scale, SafeClamp(value));
    }

    public Color FontColor
    {
        get => _fontColor;
        set => SetProperty(ref _fontColor, value);
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value);
    }

    public Color EdgeColor
    {
        get => _edgeColor;
        set => SetProperty(ref _edgeColor, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    internal record Snapshot(double Scale, Color FontColor, Color BackgroundColor, Color EdgeColor, bool Enabled);

    internal Snapshot Capture() => new(Scale, FontColor, BackgroundColor, EdgeColor, Enabled);

    internal void Restore(Snapshot snapshot)
    {
        Scale = snapshot.Scale;
        FontColor = snapshot.FontColor;
        BackgroundColor = snapshot.BackgroundColor;
        EdgeColor = snapshot.EdgeColor;
        Enabled = snapshot.Enabled;
    }

    private static double SafeClamp(double value) => double.IsFinite(value) ? Math.Clamp(value, 0.25, 4.0) : 0.25;

    private double _scale = 1.0;
    private Color _fontColor = Color.Black;
    private Color _backgroundColor = Color.White;
    private Color _edgeColor = Color.Black;
    private bool _enabled = true;
}
