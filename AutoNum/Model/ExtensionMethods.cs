using Emgu.CV;
using Emgu.CV.Structure;
using NumberIt.ViewModels;
using System.Drawing;

//using System.Windows.Media;
using ImageModel = NumberIt.ViewModels.ImageModel;

namespace AutoNumber.Model
{
    public static class ExtensionMethods
    {
        public static MCvScalar toMCvScalar(this Color col)
        {
          return new MCvScalar(col.B, col.G, col.R);  
        }

        public static Mat toNumberedMat(this ImageModel image)
        {
            Mat mat = image.emguImage!.Clone();

            var markers = image.MarkerVMs.OfType<MarkerLabel>();
                        
            int radius = (int)(MarkerLabel.Diameter / 2);
            var fontMCv = MarkerLabel.FontColor.toMCvScalar();
            var fillMCv = MarkerLabel.BackgroundColor.toMCvScalar();
            var edgeMCv = MarkerLabel.EdgeColor.toMCvScalar();

            Emgu.CV.CvEnum.FontFace fontFace = Emgu.CV.CvEnum.FontFace.HersheyDuplex;
           // Size textSize = new Size();

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

            int linewidth = (int)(fontScale * 2) + 1;

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
            return mat;
        }
    }
}
