using Emgu.CV;
using System.Drawing;

namespace NumberIt.ViewModels
{
    public class AnalyzeVM : WizardStep
    {
        public double ScaleFactor { get; set; } = 1.2;
        public int minNeighbors { get; set; } = 7;
        public int minSize { get; set; }
        public int maxSize { get; set; }

        public RelayCommand cmdAnalyze => _cmdAnalyze ??= new(doAnalyze);
        public void doAnalyze(object? o = null)
        {
            using var matt = pvm.Bitmap.ToMat();
            using var gray = new Mat();
            CvInvoke.CvtColor(matt, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

            pvm.MarkerVMs.Clear();

            var faceCascade = new CascadeClassifier("Classifiers/haarcascade_frontalface_default.xml");
            var faceMarkers = faceCascade.DetectMultiScale(gray, ScaleFactor, minNeighbors, minSize: new Size(minSize, minSize), maxSize: new Size(maxSize, maxSize));
            foreach (var faceMarker in faceMarkers)
            {
                pvm.MarkerVMs.Add(new MarkerRect
                {
                    X = faceMarker.X,
                    Y = faceMarker.Y,
                    W = faceMarker.Width,
                    H = faceMarker.Height,
                });
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
        }

        private ImageModel pvm => parent.pictureVM;
        private RelayCommand? _cmdAnalyze;
        private MainVM parent;
    }
}
