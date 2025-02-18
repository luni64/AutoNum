using Emgu.CV;
using Emgu.CV.Structure;
//using System.Windows;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace NumberIt.ViewModels
{
    public class AnalyzeVM : WizardStep
    {
        public double ScaleFactor { get; set; } = 1.2;
        public int minNeighbors { get; set; } = 7;
        public int minSize { get; set; } = 0;
        public int maxSize { get; set; } = 0;

        public RelayCommand cmdAnalyze => _cmdAnalyze ??= new(doAnalyze);
        void doAnalyze(object? o = null)
        {
            //using var EmguImage = CvInvoke.Imread(parent.ImageFilename, Emgu.CV.CvEnum.ImreadModes.Color);

            using var gray = new Mat();
            CvInvoke.CvtColor(pvm.emguImage, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

            var faceCascade = new CascadeClassifier("Classifiers/haarcascade_frontalface_default.xml");
            Rectangle[] faces = faceCascade.DetectMultiScale(gray, ScaleFactor, minNeighbors, minSize: new Size(minSize, minSize), maxSize: new Size(maxSize, maxSize));

            parent.pictureVM.MarkerVMs.Clear();
            foreach (var rect in faces)
            {
                //CvInvoke.Rectangle(EmguImage, rect, new MCvScalar(0, 255, 0), 3);
                pvm.MarkerVMs.Add(new MarkerRect
                {
                    X = rect.X,
                    Y = rect.Y,
                    W = rect.Width,
                    H = rect.Height,
                });
            }

            if (faces.Length > 0)
            {
                var av = faces.Average(f => f.Width);
                var std = faces.StdDev(f => f.Width);

            }           
        }

        public AnalyzeVM(MainVM parent)
        {
            this.parent = parent;
            
        }

        public override void Enter(object? o)
        {
            if (parent.pictureVM.MarkerVMs.Count == 0)
            {
                doAnalyze();
            }
            else
            {
                foreach (var marker in pvm.MarkerVMs)
                {
                    if (marker is MarkerRect) marker.visible = true;
                    else marker.visible = false;
                }
            }
            // parent.pictureVM.ImageSource =  bitmapSource;
        }

        private MainVM parent;
        private ImageModel pvm => parent.pictureVM;

        //  internal string ImageFile = string.Empty;

        private RelayCommand? _cmdAnalyze;
        //  private BitmapSource? bitmapSource;
    }
}
