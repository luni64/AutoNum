using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoNumber.Infrastructure
{
    public sealed class RowPreviewFillConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            var isActive = values.Length > 0 && values[0] is bool active && active;
            if (isActive)
            {
                return values.Length > 1 && values[1] is Brush previewBrush ? previewBrush : Brushes.Transparent;
            }

            return values.Length > 2 && values[2] is Brush fallbackBrush ? fallbackBrush : Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
