using Emgu.CV;
using System.Drawing;

namespace NumberIt.ViewModels
{
    public static class FaceDetector 
    {
        static public double ScaleFactor { get; set; } = 1.2;
        static public int minNeighbors { get; set; } = 7;
        static public int minSize { get; set; } = 0;
        static public int maxSize { get; set; } = 0;

        static public List<Rectangle> Detect(Bitmap bitmap)
        {
            using var matt =bitmap.ToMat();
            using var gray = new Mat();
            CvInvoke.CvtColor(matt, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                       
            var faceCascade = new CascadeClassifier("Classifiers/haarcascade_frontalface_default.xml");
            var faceMarkers = faceCascade.DetectMultiScale(gray, ScaleFactor, minNeighbors, minSize: new Size(minSize, minSize), maxSize: new Size(maxSize, maxSize));            
            return faceMarkers.ToList();
        }        
    }
}
