using System.Drawing;

namespace AutoNumber.ViewModels;

/// <summary>
/// Encapsulates the shared visual style for all label markers.
/// Replaces the former scattered static fields on <see cref="MarkerLabel"/>.
/// </summary>
public class LabelStyle : BaseViewModel
{
    public float Diameter
    {
        get => _diameter;
        set => SetProperty(ref _diameter, value);
    }

    public double FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, value);
    }

    public Color FontColor
    {
        get => _fontColor;
        set => SetProperty(ref _fontColor, value);
    }

    public Color EdgeColor
    {
        get => _edgeColor;
        set => SetProperty(ref _edgeColor, value);
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value);
    }

    public FontFamily FontFamily { get; } = new FontFamily("Calibri");

    private float _diameter;
    private double _fontSize = 12;
    private Color _fontColor = Color.Black;
    private Color _edgeColor = Color.White;
    private Color _backgroundColor = Color.Green;
}
