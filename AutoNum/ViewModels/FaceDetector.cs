using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Drawing;

namespace AutoNumber.ViewModels
{
    public static class FaceDetector
    {
        private const string ModelPath = "Classifiers/face_detection_yunet_2023mar.onnx";
        private const float ScoreThreshold = 0.7f;
        private const float NmsThreshold = 0.3f;
        private const int TopK = 5000;

        private static FaceDetectorYN? _detector;
        private static Size _detectorInputSize;

        static public List<Rectangle> Detect(Bitmap bitmap)
        {
            using var mat = ToBgrMat(bitmap);
            EnsureDetector(new Size(mat.Width, mat.Height));

            using var faces = new Mat();
            _detector!.Detect(mat, faces);

            return ExtractRectangles(faces);
        }

        /// <summary>
        /// FaceDetectorYN.Detect requires a 3-channel BGR Mat. Bitmap.ToMat() mirrors the
        /// source pixel format instead of normalizing it, so e.g. an 8bpp-indexed grayscale
        /// JPEG comes through as a 4-channel (BGRA) Mat and throws "Number of input channels
        /// should be multiple of 3" — convert whatever channel count shows up to BGR first.
        /// </summary>
        private static Mat ToBgrMat(Bitmap bitmap)
        {
            using var rawMat = bitmap.ToMat();
            if (rawMat.NumberOfChannels == 3)
            {
                return rawMat.Clone();
            }

            var conversion = rawMat.NumberOfChannels switch
            {
                1 => ColorConversion.Gray2Bgr,
                4 => ColorConversion.Bgra2Bgr,
                _ => ColorConversion.Bgra2Bgr,
            };

            var bgrMat = new Mat();
            CvInvoke.CvtColor(rawMat, bgrMat, conversion);
            return bgrMat;
        }

        private static void EnsureDetector(Size inputSize)
        {
            if (_detector is not null && _detectorInputSize == inputSize)
            {
                return;
            }

            _detector?.Dispose();
            _detector = new FaceDetectorYN(ModelPath, string.Empty, inputSize, ScoreThreshold, NmsThreshold, TopK);
            _detectorInputSize = inputSize;
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
