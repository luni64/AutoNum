using NumberIt.ViewModels;
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

        static void test()
        {
            Font font = new Font("Calibri", 12);
            var labels = new List<MarkerLabel>();

            var result = getLargesItem(labels, l => l.Name, font);
            var rm = result.d_max;
            var la = result.item;
        }
    }
}
