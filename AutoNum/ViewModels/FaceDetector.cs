using Emgu.CV;
using System.Drawing;

namespace AutoNumber.ViewModels
{
    public static class FaceDetector 
    {
        static public double ScaleFactor { get; set; } = 1.2;
        static public int MinNeighbors { get; set; } = 7;
        static public int MinSize { get; set; } = 0;
        static public int MaxSize { get; set; } = 0;

        private static readonly Lazy<CascadeClassifier> _faceCascade =
            new(() => new CascadeClassifier("Classifiers/haarcascade_frontalface_default.xml"));

        static public List<Rectangle> Detect(Bitmap bitmap)
        {
            using var matt = bitmap.ToMat();
            using var gray = new Mat();
            CvInvoke.CvtColor(matt, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

            var faceMarkers = _faceCascade.Value.DetectMultiScale(gray, ScaleFactor, MinNeighbors, minSize: new Size(MinSize, MinSize), maxSize: new Size(MaxSize, MaxSize));            
            return faceMarkers.ToList();
        }        
    }
}
