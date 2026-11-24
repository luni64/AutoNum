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

        /// <summary>
        /// Upper bound for the pixel count YuNet is run on; larger images are downscaled for
        /// detection only (the returned rectangles are always in full-image coordinates).
        /// The network runs at its input size and needs roughly 50 bytes of working memory per
        /// pixel, and OpenCV's DNN module dies with a native access violation — which no .NET
        /// handler can catch, so the app just vanishes — somewhere above 200 megapixels.
        /// Huge scans also detect worse at full size, because the faces get bigger than
        /// anything the model was trained on. 40 MP leaves ordinary camera photos untouched.
        /// </summary>
        private const long MaxDetectionPixels = 40_000_000;

        static public List<Rectangle> Detect(Bitmap bitmap)
        {
            using var fullMat = ToBgrMat(bitmap);
            using var mat = LimitDetectionSize(fullMat);
            EnsureDetector(new Size(mat.Width, mat.Height));

            using var faces = new Mat();
            _detector!.Detect(mat, faces);

            return ExtractRectangles(faces, (float)fullMat.Width / mat.Width, (float)fullMat.Height / mat.Height);
        }

        /// <summary>
        /// Returns the image to run the detector on: a downscaled copy when the image exceeds
        /// <see cref="MaxDetectionPixels"/>, otherwise a header sharing the original's data.
        /// </summary>
        private static Mat LimitDetectionSize(Mat mat)
        {
            long pixels = (long)mat.Width * mat.Height;
            if (pixels <= MaxDetectionPixels)
            {
                return new Mat(mat, new Rectangle(Point.Empty, mat.Size));
            }

            var factor = Math.Sqrt((double)MaxDetectionPixels / pixels);
            var size = new Size(Math.Max(1, (int)(mat.Width * factor)), Math.Max(1, (int)(mat.Height * factor)));
            var small = new Mat();
            CvInvoke.Resize(mat, small, size, 0, 0, Inter.Area);
            return small;
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

        /// <summary>
        /// Converts the detector output into rectangles, scaled from detection-image to
        /// full-image coordinates (both factors are 1 unless the image was downscaled).
        /// </summary>
        private static List<Rectangle> ExtractRectangles(Mat faces, float scaleX, float scaleY)
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
                    (int)MathF.Round(data[i, 0] * scaleX),
                    (int)MathF.Round(data[i, 1] * scaleY),
                    (int)MathF.Round(data[i, 2] * scaleX),
                    (int)MathF.Round(data[i, 3] * scaleY)));
            }

            return result;
        }
    }
}
