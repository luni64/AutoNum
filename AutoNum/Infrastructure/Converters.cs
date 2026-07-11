using AutoNumber.Model;
using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoNumber.Infrastructure
{
    public class BitmapToBitmapSource : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not System.Drawing.Bitmap bitmap)
            {
                return null;
            }

            // Copy the raw pixel buffer straight across (LockBits -> BitmapSource.Create).
            // The previous implementation round-tripped the whole photo through a PNG
            // encode/decode, i.e. seconds of Deflate compression per open/rotate on large
            // scans, only to immediately throw the compressed form away.
            var wpfFormat = MapPixelFormat(bitmap.PixelFormat);
            if (wpfFormat is System.Windows.Media.PixelFormat format)
            {
                var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
                try
                {
                    var source = BitmapSource.Create(
                        bitmap.Width, bitmap.Height,
                        bitmap.HorizontalResolution, bitmap.VerticalResolution,
                        format, null,
                        data.Scan0, data.Stride * bitmap.Height, data.Stride);
                    source.Freeze(); // cross-thread accessible, and detaches from the GDI buffer
                    return source;
                }
                finally
                {
                    bitmap.UnlockBits(data);
                }
            }

            // Fallback for pixel formats without a 1:1 WPF equivalent (e.g. indexed): the old
            // PNG round-trip, correct for everything even if slow.
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            memoryStream.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        /// <summary>
        /// GDI+ formats that map 1:1 onto a WPF pixel format (both are BGR byte order).
        /// Everything the app produces lands here: JPEG decode yields 24bpp RGB, composited
        /// and PNG-restored bitmaps 32bpp (P)ARGB.
        /// </summary>
        private static System.Windows.Media.PixelFormat? MapPixelFormat(System.Drawing.Imaging.PixelFormat format) => format switch
        {
            System.Drawing.Imaging.PixelFormat.Format24bppRgb => PixelFormats.Bgr24,
            System.Drawing.Imaging.PixelFormat.Format32bppRgb => PixelFormats.Bgr32,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb => PixelFormats.Bgra32,
            System.Drawing.Imaging.PixelFormat.Format32bppPArgb => PixelFormats.Pbgra32,
            _ => null
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class DrawingColToMediaBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Drawing.Color color)
            {                
                return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            }
            return System.Windows.Media.Brushes.Transparent; // Default fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush solidColorBrush)
            {
                var color = solidColorBrush.Color;
                return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
            }
            return System.Drawing.Color.Transparent; // Default fallback
        }
    }


    public class DrawingFontFamilyToMediaFontFamily : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Drawing.FontFamily fontFamily)
            {
                return new System.Windows.Media.FontFamily(fontFamily.Name);
            }
            return new System.Windows.Media.FontFamily("Arial"); // Default fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


    public class ColorConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.Color mediaColor)
            {                
                return (System.Drawing.Color?) System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
            }
            return System.Drawing.Color.Black; // Default fallback
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Drawing.Color drawingColor)
            {
                return (System.Windows.Media.Color?) System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
            }
            return (System.Windows.Media.Color?) System.Windows.Media.Colors.Black; // Default fallback
        }
    }


    public class BoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value == true) ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class CenteredGlyphOffsetConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
            {
                return 0d;
            }

            var text = values[0]?.ToString();
            var fontFamily = values[1] as FontFamily;
            if (string.IsNullOrWhiteSpace(text) || fontFamily is null || values[2] is not double fontSize || fontSize <= 0)
            {
                return 0d;
            }

            var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var formattedText = new FormattedText(
                text,
                culture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                1.0);

            var bounds = formattedText.BuildGeometry(new Point(0, 0)).Bounds;
            var lineBoxCenter = formattedText.Height / 2.0;
            var glyphCenter = bounds.Y + bounds.Height / 2.0;
            return lineBoxCenter - glyphCenter;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class EnumBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string? parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string? parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            // RadioButton also fires ConvertBack(false, ...) for the button that just got
            // unchecked when another one in the group is selected; ignore that or it
            // immediately resets the bound enum back to the old value.
            if (value is bool isChecked && !isChecked)
                return Binding.DoNothing;

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }

    public class NameTableNumberColumnWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double rowWidth && double.IsFinite(rowWidth) && rowWidth > 0)
            {
                return (double)NamesTableLayout.ResolveNumberColumnWidth(rowWidth);
            }

            return 0d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class NameTableNameColumnWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double rowWidth && double.IsFinite(rowWidth) && rowWidth > 0)
            {
                var numberColumnWidth = NamesTableLayout.ResolveNumberColumnWidth(rowWidth);
                return Math.Max(1d, rowWidth - numberColumnWidth);
            }

            return 1d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    internal static class SliderScaleMapping
    {
        public const double SliderMin = 0.0;
        public const double SliderMax = 1.0;
        public const double SliderDefault = 0.5;
        public const double ScaleMin = 0.25;
        public const double ScaleMax = 4.0;
        public const double ScaleDefault = 1.0;

        public static double SliderToScale(double sliderPosition)
        {
            var clamped = Math.Clamp(sliderPosition, SliderMin, SliderMax);
            return 0.25 * Math.Pow(16.0, clamped);
        }

        public static double ScaleToSlider(double scale)
        {
            if (!double.IsFinite(scale) || scale <= 0)
            {
                return SliderDefault;
            }

            var ratio = scale / 0.25;
            var x = Math.Log(ratio) / Math.Log(16.0);
            return Math.Clamp(x, SliderMin, SliderMax);
        }
    }

    /// <summary>
    /// Converts between slider position (0–1) and scale factor (0.25–4.0).
    /// </summary>
    public class SliderToScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double scale && double.IsFinite(scale) && scale > 0)
            {
                return SliderScaleMapping.ScaleToSlider(scale);
            }

            return SliderScaleMapping.SliderDefault;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double sliderPosition && double.IsFinite(sliderPosition))
            {
                return SliderScaleMapping.SliderToScale(sliderPosition);
            }

            return SliderScaleMapping.ScaleDefault;
        }
    }

}

