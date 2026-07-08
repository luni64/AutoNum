using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoNumber.Infrastructure;

public sealed class RowPreviewEdgeConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
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
