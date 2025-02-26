using Emgu.CV;
using Emgu.CV.Structure;
using NumberIt.ViewModels;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Windows.Controls;





//using ImageModel = NumberIt.ViewModels.ImageModel;


namespace AutoNumber.Model
{
    public static class ExtensionMethods
    {
        public static MCvScalar toMCvScalar(this Color col)
        {
            return new MCvScalar(col.B, col.G, col.R);
        }

        public static Bitmap toNumberedBitmap(this ImageModel image)
        {
            var labels = image.MarkerVMs.OfType<MarkerLabel>().ToList();

            var bmpFinal = (Bitmap)image.Bitmap.Clone();
            using var gFinal = Graphics.FromImage(bmpFinal);

            float gdiFontSize = 0.7f * (float)MarkerLabel.FontSize * 96 / gFinal.DpiX;

            using Pen edgePen = new Pen(MarkerLabel.EdgeColor, 3);
            using Brush fillBrush = new SolidBrush(MarkerLabel.BackgroundColor);
            using Brush textBrush = new SolidBrush(MarkerLabel.FontColor);
            using Font font = new Font("Calibri", gdiFontSize);

            StringFormat format = new StringFormat(StringFormat.GenericDefault);

            foreach (var label in labels)
            {
                PointF circlePos = new PointF((float)label.X, (float)label.Y);
                SizeF circleSize = new SizeF(MarkerLabel.Diameter, MarkerLabel.Diameter);
                RectangleF BB = new RectangleF(circlePos, circleSize);

                gFinal.FillEllipse(fillBrush, BB);
                gFinal.DrawEllipse(edgePen, BB);

                SizeF textSize = gFinal.MeasureString(label.Number, font, circlePos, format);
                PointF textPos = new(circlePos.X + (BB.Width - textSize.Width) / 2.0f, circlePos.Y + (BB.Height - textSize.Height) / 2.0f);

                gFinal.DrawString(label.Number, font, Brushes.Black, textPos, format);
            }

            var names = labels.Select(l => $"{l.Number}) {l.Name}").ToList();

            var largestLabel = getLargestLabelWidth(names, bmpFinal, font, gFinal) + MarkerLabel.Diameter / 2;
            int nrOfCols = Math.Max(1, (int)Math.Floor(bmpFinal.Width / largestLabel));
            float colWidth = bmpFinal.Width / nrOfCols;

            int col = 0;
            int row = 0;
            foreach (var name in names)
            {
                PointF textPos = new(col * colWidth, row * font.Height * 2);
                gFinal.DrawString(name, font, Brushes.Black, textPos);
                col++;
                if (col >= nrOfCols)
                {
                    col = 0;
                    row++;
                }
            }



            return bmpFinal;
        }
        static float getLargestLabelWidth(IEnumerable<string> names, Bitmap bitmap, Font font, Graphics g)
        {
            float maxSize = 0;

            foreach (var name in names)
            {
                // using var gFinal = Graphics.FromImage(bitmap);
                SizeF textSize = g.MeasureString(name, font);
                if (textSize.Width > maxSize)
                {
                    maxSize = textSize.Width;
                }
            }
            return maxSize;
        }

        public static Mat toNumberedMat(this ImageModel image)
        {
            Mat mat = new();



            CvInvoke.Resize(image.Bitmap.ToMat(), mat, new Size(), 1, 1, Emgu.CV.CvEnum.Inter.LinearExact);


            var markers = image.MarkerVMs.OfType<MarkerLabel>().OrderBy(m => int.Parse(m.Number));

            int radius = (int)(MarkerLabel.Diameter / 2);
            var fontMCv = MarkerLabel.FontColor.toMCvScalar();
            var fillMCv = MarkerLabel.BackgroundColor.toMCvScalar();
            var edgeMCv = MarkerLabel.EdgeColor.toMCvScalar();

            Emgu.CV.CvEnum.FontFace fontFace = Emgu.CV.CvEnum.FontFace.HersheySimplex;
            // circleSize textSize = new circleSize();

            string widestString = "";

            int baseline = 0; // distance (in pixels) from the bottom-most text point to the actual text baseline, output param!

            // finds the largest number text in the markers collection
            var maxSize = new Size();
            foreach (var marker in markers)
            {
                Size tempSize = CvInvoke.GetTextSize(marker.Number, fontFace, 1, 1, ref baseline);
                if (tempSize.Width > maxSize.Width)
                {
                    maxSize = tempSize;
                    widestString = marker.Number;
                }
            }

            double fontScale = 0;
            for (double scale = 10; scale > 0.1; scale -= 0.01)
            {
                Size tempSize = CvInvoke.GetTextSize(widestString, fontFace, scale, 1, ref baseline);
                if (tempSize.Width <= 2 * radius * 0.7 && tempSize.Height <= 2 * radius * 0.7)
                {
                    fontScale = scale;
                    break;
                }
            }

            int linewidth = (int)(fontScale) + 1;

            var AA = Emgu.CV.CvEnum.LineType.AntiAlias;

            foreach (var marker in markers)
            {
                int edgeThickness = (int)marker.StrokeThickness;
                Point center = new((int)marker.X + radius, (int)marker.Y + radius);

                CvInvoke.Circle(mat, center, radius, fillMCv, -1, AA);
                CvInvoke.Circle(mat, center, radius, edgeMCv, edgeThickness, AA);


                // Draw number in the center of the circle
                Size textSize = CvInvoke.GetTextSize(marker.Number, fontFace, fontScale, 1, ref baseline);
                Point textOrg = new Point(
                    center.X - (textSize.Width / 2),
                    center.Y + (textSize.Height / 2)
                );

                // Draw the number
                CvInvoke.PutText(mat, marker.Number, textOrg, fontFace, fontScale, fontMCv, linewidth, AA);

            }






            var ml = markers.ToList();

            for (int i = 0; i < ml.Count; i++)
            {
                ml[i].Name = names[i];
            }

            return mat.ToBitmap().AddNamesMultiColumnOptimized(ml).ToMat();

            return mat;
        }
         public static List<string> names { get; } = new List<string>
            {
            "Hans Müller",
            "Anna Schmidt",
            "Peter Fischer",
            "Sabine Schneider",
            "Thomas Meyer",
            "Julia Wagner",
            "Markus Weber",
            "Lena Becker",
            "Stefan Hoffmann",
            "Claudia Schäfer",
            "Jan Schmid",
            "Johanna Braun",
            "Michael Wolf",
            "Franziska Richter",
            "Tobias Baumann",
            "Lisa Koch",
            "Christian Neumann",
            "Katharina Krause",
            "Lukas Vogel",
            "Susanne Schubert",
            "Matthias Lehmann",
            "Nicole Schäfer",
            "Alexander Schulz",
            "Clara Bauer",
            "Florian Roth",
            "Carolin Haas",
            "Patrick Keller",
            "Laura Maier",
            "Sebastian Groß",
            "Marie Hartmann",
            "Jonas Lang",
            "Anja Schmitt",
            "Benjamin Kaiser",
            "Vanessa Böhm",
            "Moritz Peters",
            "Andrea Frank",
            "Johannes Brandt",
            "Melanie Winter",
            "Tim Arnold",
            "Verena Bergmann",
            "Erik Albrecht",
            "Jutta Schuster",
            "Hendrik Beck",
            "Nadine Heinemann",
            "Philipp König",
            "Maren Simon",
            "Oliver Hermann",
            "Sarah Seidel",
            "Maximilian Fuchs",
            "Helena Lorenz",
            "Dominik Moser",
            "Isabell Horn",
            "Jonas Weiß",
            "Theresa Gebhardt",
            "Christoph Auer",
            "Lisa Schädtler",
            "Paul Metzger",
            "Nora Blum",
            "Felix Kroll",
            "Amelie Schneider",
            "Konstantin Engel",
            "Johanna Kraemer",
            "Simon Herzog",
            "Pauline Urban",
            "Daniel Kraft",
            "Miriam Seyfried",
            "Leon Rieger",
            "Carolin Busch",
            "Andreas Schwarz",
            "Viktoria Wendt",
            "Theo Meier",
            "Rebecca Hübner",
            "Martin Böhmer",
            "Hanna Vogt",
            "Julian Weiss",
            "Paula Siebert",
            "Marcel Bischof",
            "Annalena Dietrich",
            "Lukas Ziegler",
            "Magdalena Paul",
            "Rico Schauer",
            "Nadine Vogler",
            "Fabian Hein",
            "Tanja Sommer",
            "Kilian Reinhardt",
            "Jasmin Lindner",
            "Nico Pohl",
            "Friederike Krämer",
            "Viktor Schlegel",
            "Lea Brunner",
            "Sven Henning",
            "Rebecca Bruns",
            "Leonhard Oster",
            "Eva Voigt",
            "Christoph Bayer",
            "Maria Franke",
            "Florian Mayer",
            "Theresa Ludwig",
            "Jonas Reuter",
            "Sara Pfeiffer"
        };
    }
}
