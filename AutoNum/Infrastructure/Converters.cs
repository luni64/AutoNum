using Emgu.CV;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
//using System.Windows.Media.Imaging;

namespace NumberIt.Infrastructure
{
    public class BitmapToBitmapSource : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Drawing.Bitmap bitmap)
            {
                if (bitmap == null) return null;                

                using (var memoryStream = new MemoryStream())
                {
                    // Save the Bitmap to the stream
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    memoryStream.Position = 0;

                    // Create a BitmapImage from the stream
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // Freeze to make it cross-thread accessible

                    return bitmapImage;
                }               
            }
            return null;


            //if (value is Emgu.CV.Mat mat)
            //{
            //    return mat.ToBitmapSource();
            //}
            //return null;
        }

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


    public class BoolToObjectConverter : MarkupExtension, IValueConverter
    {
        //public PackIconBase checkedIcon { get; set; } = new PackIconModern() {Kind = PackIconModernKind.Creditcard };
        //public PackIconBase uncheckedIcon { get; set; } = new PackIconModern() { Kind = PackIconModernKind.Coupon };
        public object? checkedIcon { get; set; }
        public object? uncheckedIcon { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOn)
            {
                return isOn ? checkedIcon : uncheckedIcon;
            }
            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    //public class BoolToObjectConverter2 : IValueConverter
    //{
    //    //PackIconMaterialKind checkedIconKind = PackIconMaterialKind.Star;
    //    //PackIconBase uncheckedIcon = new PackIconMaterial() { Kind = PackIconMaterialKind.Star,Width = 10, Height = 10 };

    //    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return (bool)value ? PackIconMaterialKind.Star : PackIconMaterialKind.StarOutline;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotSupportedException();
    //    }
    //}

    public class StringToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object? result = null;
            var path = value as string;

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                using (var stream = File.OpenRead(path))
                {
                    var image = new System.Windows.Media.Imaging.BitmapImage();
                    image.BeginInit();
                    image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    result = image;
                }
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    //public class StringToImageWidth : IValueConverter
    //{
    //    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        object? result = null;
    //        var path = value as string;

    //        if (!string.IsNullOrEmpty(path) && File.Exists(path))
    //        {
    //            using (var stream = File.OpenRead(path))
    //            {
    //                var image = new BitmapImage();
    //                image.BeginInit();
    //                image.CacheOption = BitmapCacheOption.OnLoad;
    //                image.StreamSource = stream;
    //                image.EndInit();
    //                result = image;
    //            }
    //        }
    //        return result;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotSupportedException();
    //    }
    //}

    //public class BookTypeToIconConverter : IValueConverter
    //{
    //    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value is BookType bookType && parameter is string size)
    //        {
    //            int sz = int.Parse(size);
    //            return bookType switch
    //            {
    //                BookType.Sterbebücher => new PackIconPhosphorIcons() { Kind = PackIconPhosphorIconsKind.CrossBold, Width = sz, Height = sz },
    //                BookType.Hochzeitsbücher => new PackIconMaterial() { Kind = PackIconMaterialKind.HumanMaleFemale, Width = sz, Height = sz },
    //                //BookType.Taufbücher => new PackIconModern() { Kind = PackIconModernKind.Baby, Width = sz, Height = sz },
    //                BookType.Taufbücher => new PackIconMaterial() { Kind = PackIconMaterialKind.BabyCarriage, Width = sz, Height = sz },
    //                _ => null
    //            };
    //        }
    //        return null;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotSupportedException();
    //    }
    //}

    public class intToMarginConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return new Thickness(0);
            return new Thickness((int)value, 0, 0, 0);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class nullToEnabledConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class nullToVisibiltiyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class boolToVisibiltiyConverter : IValueConverter
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

    public class ComparisonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.Equals(parameter) ?? false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
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

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }

}
