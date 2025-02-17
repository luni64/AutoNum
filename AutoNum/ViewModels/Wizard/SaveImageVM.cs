//using System.Windows;
using Emgu.CV;
using Emgu.CV.Structure;
using NumberIt.Views;
using System.Drawing;
using System.IO;
//using System.Windows;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;



namespace NumberIt.ViewModels
{
    public class SaveImageVM : WizardStep
    {
        private RelayCommand? _cmdSaveImage;
        public RelayCommand cmdSaveImage => _cmdSaveImage ??= new(doSaveImage);



        public string outputFile { get; set; }
        void doSaveImage(object? o)
        {
            var pvm = parent.pictureVM;

            if (parent.DialogService.ShowDialog(this) is string filename && !string.IsNullOrEmpty(filename))
            {
                using Mat mat = pvm.emguImage!.Clone();

                var markers = pvm.MarkerVMs.OfType<MarkerLabel>();

                var fontColor = (MarkerLabel.ForegroundBrush as SolidColorBrush).Color;
                var fillColor = (MarkerLabel.BackgroundBrush as SolidColorBrush).Color;
                var edgeColor = (MarkerLabel.EdgeBrush as SolidColorBrush).Color;
                int radius = (int)(MarkerLabel.Diameter / 2);

                var fillMCv = new MCvScalar(fillColor.B, fillColor.G, fillColor.R);  // doesn't support alpha channel
                var edgeMCv = new MCvScalar(edgeColor.B, edgeColor.G, edgeColor.R);
                var fontMCv = new MCvScalar(fontColor.B, fontColor.G, fontColor.R); // doesn't support alpha channel

                Emgu.CV.CvEnum.FontFace fontFace = Emgu.CV.CvEnum.FontFace.HersheySimplex;
                int baseline = 1;
                Size textSize = new Size();


             
                string s = "";

                Size ms = new Size();

                foreach (var marker in markers)
                {
                    Size tempSize = CvInvoke.GetTextSize(marker.Number, fontFace, 1, 1, ref baseline);
                    if (tempSize.Width > ms.Width)
                    {
                        ms = tempSize;
                        s = marker.Number;
                    }
                }

                double fontScale = 0;

                for (double scale = 10; scale > 0.1; scale -= 0.01)
                {
                    Size tempSize = CvInvoke.GetTextSize(s, fontFace, scale, 1, ref baseline);
                    if (tempSize.Width <= 2*radius * 0.7 && tempSize.Height <= 2*radius * 0.7)
                    {
                        fontScale = scale;
                        textSize = tempSize;
                        break;
                    }                    
                }

                int linewidth = (int)( fontScale * 2)+1;


                var AA = Emgu.CV.CvEnum.LineType.AntiAlias;

                foreach (var marker in markers)
                {
                    int edgeThickness = (int)marker.StrokeThickness;
                    Point center = new((int)marker.X + radius, (int)marker.Y + radius);

                    CvInvoke.Circle(mat, center, radius, fillMCv, -1, AA);
                    CvInvoke.Circle(mat, center, radius, edgeMCv, edgeThickness,AA);

                    // Initialize variables for font scale and text size


                    // Choose the font face

                    // Determine the maximum font scale that fits the text within the circle
                    //for (double scale = 10; scale > 0.1; scale -= 0.05)
                    //{
                    //    Size tempSize = CvInvoke.GetTextSize(marker.Number, fontFace, scale, 1, ref baseline);
                    //    if (tempSize.Width <= (radius) * 0.7 && tempSize.Height <= (2 * radius) * 0.7)
                    //    {
                    //        fontScale = scale;
                    //        textSize = tempSize;
                    //        break;
                    //    }
                    //    //else
                    //    //{
                    //    //    break;
                    //    //}
                    //}

                    // Calculate the bottom-left corner of the text to center it

                    Size tempSize = CvInvoke.GetTextSize(marker.Number, fontFace, fontScale, 1, ref baseline);

                    Point textOrg = new Point(
                        center.X - (tempSize.Width / 2),
                        center.Y + (tempSize.Height / 2)
                    );

                    // Draw the text
                    CvInvoke.PutText(mat, marker.Number, textOrg, fontFace, fontScale, fontMCv, linewidth,AA);


                }

                mat?.Save(filename);
            }
        }


        public SaveImageVM(MainVM parent)
        {
            this.parent = parent;

        }

        public override void Enter(object? o)
        {
            var filename = Path.GetFileNameWithoutExtension(parent.pictureVM.Filename);
            var extension = Path.GetExtension(parent.pictureVM.Filename);
            var folder = Path.GetDirectoryName(parent.pictureVM.Filename);

            outputFile = Path.Combine(folder, filename + "_NUM" + extension);

        }

        private MainVM parent;
    }
}
