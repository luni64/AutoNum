using Emgu.CV;
using System.Drawing;

namespace AutoNumber.ViewModels
{
    public static class FaceDetector
    {
        private const string ModelPath = "Classifiers/face_detection_yunet_2023mar.onnx";
        private const float NmsThreshold = 0.3f;
        private const int TopK = 5000;

        static public float ScoreThreshold { get; set; } = 0.7f;

        private static FaceDetectorYN? _detector;
        private static Size _detectorInputSize;
        private static float _detectorScoreThreshold;

        static public List<Rectangle> Detect(Bitmap bitmap)
        {
            using var mat = bitmap.ToMat();
            EnsureDetector(new Size(mat.Width, mat.Height));

            using var faces = new Mat();
            _detector!.Detect(mat, faces);

            return ExtractRectangles(faces);
        }

        private static void EnsureDetector(Size inputSize)
        {
            if (_detector is not null && _detectorInputSize == inputSize && _detectorScoreThreshold == ScoreThreshold)
            {
                return;
            }

            _detector?.Dispose();
            _detector = new FaceDetectorYN(ModelPath, string.Empty, inputSize, ScoreThreshold, NmsThreshold, TopK);
            _detectorInputSize = inputSize;
            _detectorScoreThreshold = ScoreThreshold;
        }

        private static List<Rectangle> ExtractRectangles(Mat faces)
        {
            var result = new List<Rectangle>();
            if (faces.Rows == 0)
            {
                return result;
            }

            var data = (float[,])faces.GetData();
            for (int i = 0; i < faces.Rows; i++)
            {
                result.Add(new Rectangle(
                    (int)MathF.Round(data[i, 0]),
                    (int)MathF.Round(data[i, 1]),
                    (int)MathF.Round(data[i, 2]),
                    (int)MathF.Round(data[i, 3])));
            }

            return result;
        }
    }
}
