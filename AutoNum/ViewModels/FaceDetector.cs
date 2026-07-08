using Emgu.CV;
using System.Diagnostics;
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
            var sw = Stopwatch.StartNew();

            // Force classifier load here so its (one-time) cost is not attributed to DetectMultiScale below.
            var cascade = _faceCascade.Value;
            var cascadeLoadMs = sw.ElapsedMilliseconds;

            sw.Restart();
            using var matt = bitmap.ToMat();
            using var gray = new Mat();
            CvInvoke.CvtColor(matt, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
            var convertMs = sw.ElapsedMilliseconds;

            sw.Restart();
            var faceMarkers = cascade.DetectMultiScale(gray, ScaleFactor, MinNeighbors, minSize: new Size(MinSize, MinSize), maxSize: new Size(MaxSize, MaxSize));
            var detectMs = sw.ElapsedMilliseconds;

            Trace.WriteLine($"FaceDetector.Detect: image={bitmap.Width}x{bitmap.Height}, cascadeLoad={cascadeLoadMs}ms, bgr2gray={convertMs}ms, detectMultiScale={detectMs}ms, faces={faceMarkers.Length}, scaleFactor={ScaleFactor}, minNeighbors={MinNeighbors}, minSize={MinSize}, maxSize={MaxSize}");

            return faceMarkers.ToList();
        }
    }
}
