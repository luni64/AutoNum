using Emgu.CV;
using Emgu.CV.Structure;
using NumberIt.ViewModels;
using System.Drawing;
using System.Windows.Media;
using ImageModel = NumberIt.ViewModels.ImageModel;

namespace AutoNumber.Model
{
    public static class ExtensionMethods
    {
        public static Mat toNumberedMat(this ImageModel image)
        {
            Mat mat = image.emguImage!.Clone();

            var markers = image.MarkerVMs.OfType<MarkerLabel>();

            var fontColor = (MarkerLabel.ForegroundBrush as SolidColorBrush).Color;
            var fillColor = (MarkerLabel.BackgroundBrush as SolidColorBrush).Color;
            var edgeColor = (MarkerLabel.EdgeBrush as SolidColorBrush).Color;
            int radius = (int)(MarkerLabel.Diameter / 2);

            var fillMCv = new MCvScalar(fillColor.B, fillColor.G, fillColor.R);  // doesn't support alpha channel
            var edgeMCv = new MCvScalar(edgeColor.B, edgeColor.G, edgeColor.R);
            var fontMCv = new MCvScalar(fontColor.B, fontColor.G, fontColor.R); // doesn't support alpha channel

            Emgu.CV.CvEnum.FontFace fontFace = Emgu.CV.CvEnum.FontFace.HersheyDuplex;
            Size textSize = new Size();

            string s = "";

            int baseline = 0; // distance (in pixels) from the bottom-most text point to the actual text baseline, output param!

            // finds the largest number text in the markers collection
            var maxSize = new Size();
            foreach (var marker in markers)
            {
                Size tempSize = CvInvoke.GetTextSize(marker.Number, fontFace, 1, 1, ref baseline);
                if (tempSize.Width > maxSize.Width)
                {
                    maxSize = tempSize;
                    s = marker.Number;
                }
            }

            double fontScale = 0;

            for (double scale = 10; scale > 0.1; scale -= 0.01)
            {
                Size tempSize = CvInvoke.GetTextSize(s, fontFace, scale, 1, ref baseline);
                if (tempSize.Width <= 2 * radius * 0.7 && tempSize.Height <= 2 * radius * 0.7)
                {
                    fontScale = scale;
                    textSize = tempSize;
                    break;
                }
            }

            int linewidth = (int)(fontScale * 2) + 1;


            var AA = Emgu.CV.CvEnum.LineType.AntiAlias;

            foreach (var marker in markers)
            {
                int edgeThickness = (int)marker.StrokeThickness;
                Point center = new((int)marker.X + radius, (int)marker.Y + radius);

                CvInvoke.Circle(mat, center, radius, fillMCv, -1, AA);
                CvInvoke.Circle(mat, center, radius, edgeMCv, edgeThickness, AA);



                // Calculate the bottom-left corner of the text to center it

                Size tempSize = CvInvoke.GetTextSize(marker.Number, fontFace, fontScale, 1, ref baseline);

                Point textOrg = new Point(
                    center.X - (tempSize.Width / 2),
                    center.Y + (tempSize.Height / 2)
                );

                // Draw the text
                CvInvoke.PutText(mat, marker.Number, textOrg, fontFace, fontScale, fontMCv, linewidth, AA);



            }
            return mat;


        }
    }
}
