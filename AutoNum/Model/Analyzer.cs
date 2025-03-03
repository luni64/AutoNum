using NumberIt.ViewModels;
using System.ComponentModel;
using System.Drawing;


namespace NumberIt.Model
{
    public static class Analyzer
    {
        static Graphics g = Graphics.FromImage(new Bitmap(1, 1));

        public static float getCircumscribingDiameter(string s, Font font)
        {
            SizeF size = g.MeasureString(s, font);
            return (float)Math.Sqrt(size.Width * size.Width + size.Height * size.Height);
        }

        public static (float d_max, T? item) getLargesItem<T>(IEnumerable<T> list, Func<T, string?> selector, Font font)
        {
            float d_max = 0;
            T? largestItem = list.FirstOrDefault();
            foreach (T item in list)
            {
                string? str = selector(item);
                if (str == null) continue;

                var r = getCircumscribingDiameter(str, font);
                if (r > d_max)
                {
                    d_max = r;
                    largestItem = item;
                }
            }
            return (d_max, largestItem);
        }


        public static SizeF getItemsBoundingBox<T>(ICollectionView list, Func<T, string?> selector, Font font)
        {
            g.PageUnit = GraphicsUnit.Point;
            SizeF largest = new SizeF();
            foreach (T item in list)
            {
                string? str = selector(item);
                if (str == null) continue;

                SizeF thisSize = g.MeasureString(str, font);
                largest.Width = Math.Max(largest.Width, thisSize.Width);
                largest.Height = Math.Max(largest.Height, thisSize.Height);
            }
            return largest;
        }


        public static double PlaceTitle(TitleManager tm)
        {
            g.PageUnit = GraphicsUnit.Point;

            using var font = new Font(MarkerLabel.FontFamily, (int) tm.TitleFontSize);
            SizeF thisSize = g.MeasureString(tm.Title, font);
            return thisSize.Height;
        }

        public static double PlacePersonNames(ICollectionView persons, double width, double height)
        {
            using Font font = new Font(TextLabel.fontFamily, (float)TextLabel.FontSize);

            var bb = getItemsBoundingBox<Person>(persons, p => !string.IsNullOrEmpty(p.Name.Text) ? p.FullName : "______________", font);
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
                person.Name.visible = true;
            }
            return (Math.Max(1, rowNr + 1) * bb.Height);
        }
    }
}
