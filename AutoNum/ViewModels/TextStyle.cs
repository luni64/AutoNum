using System.Drawing;

namespace AutoNumber.ViewModels;

/// <summary>
/// Encapsulates the shared visual style for all text-name labels.
/// Replaces the former scattered static fields on <see cref="TextLabel"/>.
/// </summary>
public class TextStyle : BaseViewModel
{
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

    public FontFamily FontFamily
    {
        get => _fontFamily;
        set => SetProperty(ref _fontFamily, value);
    }

    private double _fontSize = 12;
    private Color _fontColor = Color.Black;
    private FontFamily _fontFamily = new("Calibri");
}
