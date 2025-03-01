﻿using NumberIt.ViewModels;
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


        public static SizeF getItemsBoundingBox<T>(IEnumerable<T> list, Func<T, string?> selector, Font font)
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



        static void test()
        {
            Font font = new Font("Calibri", 12);
            var labels = new List<MarkerLabel>();

            var result = getLargesItem(labels, l => l.Name, font);
            var rm = result.d_max;
            var la = result.item;
        }

        public static void PlacePersonNames(IEnumerable<TextLabel> names, double width,double height)
        {
            //Font font = new Font(TextLabel.fontFamily, (float)TextLabel.FontSize);

            Font font = new Font(TextLabel.fontFamily, (float)TextLabel.FontSize);

            var bb = getItemsBoundingBox(names, n => n.Text, font);
            var nrOfColumns = Math.Floor(width / bb.Width);

            int colNr = 0;
            int rowNr = 0; 
            foreach(var name in names)
            {
                name.X = colNr * bb.Width;
                name.Y = height + rowNr * bb.Height;
                name.W = bb.Width;
                name.H = bb.Height;
                colNr++;
                if (colNr == nrOfColumns)
                {
                    rowNr++;
                    colNr = 0;
                }
                name.visible = true;
            }

        }
    }
}
