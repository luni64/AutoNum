using AutoNumber.ViewModels;
using System.ComponentModel;
using System.Drawing;


namespace AutoNumber.Model
{
    public static class Analyzer
    {
        private static SizeF measureString(string text, Font font, GraphicsUnit unit = GraphicsUnit.Display)
        {
            using var bmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bmp);
            g.PageUnit = unit;
            return g.MeasureString(text, font);
        }

        public static float GetCircumscribingDiameter(string s, Font font)
        {
            SizeF size = measureString(s, font);
            return (float)Math.Sqrt(size.Width * size.Width + size.Height * size.Height);
        }

        public static (float d_max, T? item) GetLargestItem<T>(IEnumerable<T> list, Func<T, string?> selector, Font font)
        {
            float d_max = 0;
            T? largestItem = list.FirstOrDefault();
            foreach (T item in list)
            {
                string? str = selector(item);
                if (str is null) continue;

                var r = GetCircumscribingDiameter(str, font);
                if (r > d_max)
                {
                    d_max = r;
                    largestItem = item;
                }
            }
            return (d_max, largestItem);
        }

        public static SizeF GetLargestBoundingBox<T>(IEnumerable<T> items, Func<T, string?> selector, Font font)
        {
            SizeF largest = new SizeF();
            foreach (T item in items)
            {
                string? str = selector(item);
                if (str is null) continue;

                SizeF thisSize = measureString(str, font, GraphicsUnit.Point);
                largest.Width = Math.Max(largest.Width, thisSize.Width);
                largest.Height = Math.Max(largest.Height, thisSize.Height);
            }
            return largest;
        }

        public static double GetTextBlockHeight(string text, FontFamily fontFamily, double fontSize)
        {
            if (string.IsNullOrWhiteSpace(text) || !double.IsFinite(fontSize) || fontSize <= 0)
            {
                return 0;
            }

            using var font = new Font(fontFamily, (float)fontSize);
            SizeF thisSize = measureString(text, font, GraphicsUnit.Point);
            return thisSize.Height;
        }

        public static double PlaceTitle(TitleManager tm, ImageInfoManager iim)
        {
            return GetTextBlockHeight(tm.Title, tm.TitleFontFamily, tm.TitleFontSize)
                + GetTextBlockHeight(iim.ImageInfo, iim.ImageInfoFontFamily, iim.ImageInfoFontSize);
        }

        public static double PlacePersonNames(ICollectionView persons, double width, double startY)
        {
            var fontSize = double.IsFinite(TextLabel.Style.FontSize) && TextLabel.Style.FontSize > 0
                ? TextLabel.Style.FontSize
                : 1.0;

            using Font font = new Font(TextLabel.Style.FontFamily, (float)fontSize);

            var bb = GetLargestBoundingBox<Person>(persons.OfType<Person>(), p => !string.IsNullOrEmpty(p.Name.Text) ? p.Name.Text : "______________", font);
            var rowHeight = Math.Max(NamesTableLayout.BitmapMinRowHeight, bb.Height + 2 * NamesTableLayout.BitmapCellPaddingY);
            var tableWidth = Math.Max(1.0, width);

            int rowNr = 0;
            foreach (Person person in persons)
            {
                person.Name.X = 0;
                person.Name.Y = startY + rowNr * rowHeight;
                person.Name.W = tableWidth;
                person.Name.H = rowHeight;
                rowNr++;
                person.Name.Visible = true;
            }

            return Math.Max(1, rowNr) * rowHeight;
        }
    }
}
