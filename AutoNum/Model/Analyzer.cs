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

        public static double PlaceTitle(TitleManager tm)
        {
            using var font = new Font(MarkerLabel.FontFamily, (int) tm.TitleFontSize);
            SizeF thisSize = measureString(tm.Title, font, GraphicsUnit.Point);
            return thisSize.Height;
        }

        public static double PlacePersonNames(ICollectionView persons, double width, double height)
        {
            using Font font = new Font(TextLabel.FontFamily, (float)TextLabel.FontSize);

            var bb = GetLargestBoundingBox<Person>(persons.OfType<Person>(), p => !string.IsNullOrEmpty(p.Name.Text) ? p.FullName : "______________", font);
            var nrOfColumns = (int)Math.Floor(width / bb.Width);

            int colNr = 0;
            int rowNr = 0;
            foreach (Person person in persons)
            {
                person.Name.X = colNr * bb.Width;
                person.Name.Y = height + rowNr * bb.Height + bb.Height/8;
                person.Name.W = bb.Width;
                person.Name.H = bb.Height;
                colNr++;
                if (colNr == nrOfColumns)
                {
                    rowNr++;
                    colNr = 0;
                }
                person.Name.Visible = true;
            }
            return (Math.Max(1, rowNr + 1) * bb.Height);
        }
    }
}
