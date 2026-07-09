using System.Globalization;
using System.Windows.Data;

namespace AutoNumber.Infrastructure;

public sealed class SelectedStrokeThicknessConverter : IMultiValueConverter
{
    private const double SelectedThicknessFraction = 0.15;

    public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var baseThickness = values.Length > 0 && values[0] is double thickness ? thickness : 0.0;
        var isSelected = values.Length > 2 && values[2] is bool selected && selected;
        if (!isSelected)
        {
            return baseThickness;
        }

        var diameter = values.Length > 1 && values[1] is IConvertible c ? c.ToDouble(culture) : 0.0;
        return diameter * SelectedThicknessFraction;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
