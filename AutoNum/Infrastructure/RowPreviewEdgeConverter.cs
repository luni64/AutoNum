using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoNumber.Infrastructure;

public sealed class RowPreviewEdgeConverter : IMultiValueConverter
{
    // Selection ring color: a fixed neon magenta so it stands out against portraits, which are
    // rarely magenta, regardless of the label's own edge/background color choice.
    private static readonly SolidColorBrush SelectionBrush = new(Color.FromRgb(0xFF, 0x00, 0xE5));

    public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isSelected = values.Length > 3 && values[3] is bool selected && selected;
        if (isSelected)
        {
            return SelectionBrush;
        }

        var isActive = values.Length > 0 && values[0] is bool active && active;
        if (isActive && values.Length > 1 && values[1] is SolidColorBrush previewBrush)
        {
            var color = previewBrush.Color;
            return new SolidColorBrush(Color.FromArgb(
                color.A,
                (byte)Math.Clamp((int)(color.R * 0.68), 0, 255),
                (byte)Math.Clamp((int)(color.G * 0.68), 0, 255),
                (byte)Math.Clamp((int)(color.B * 0.68), 0, 255)));
        }

        return values.Length > 2 && values[2] is Brush fallbackBrush ? fallbackBrush : Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
