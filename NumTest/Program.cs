using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;

class FaceDetector
{
    static void Main()
    {
        // 1. Load an image
        Mat image = CvInvoke.Imread("test1.jpg", Emgu.CV.CvEnum.ImreadModes.Color);

        // 2. Convert to grayscale (face detection typically works on grayscale images)
        using (Mat gray = new Mat())
        {
            CvInvoke.CvtColor(image, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

            // 3. Perform face detection using a Haar cascade
            //    (You can download haarcascade_frontalface_default.xml from the OpenCV GitHub)
            var faceCascade = new CascadeClassifier("haarcascade_frontalface_default.xml");
            Rectangle[] faces = faceCascade.DetectMultiScale(gray, 1.2, 6, new Size(100, 100));

            // 4. Draw rectangles around detected faces
            foreach (var rect in faces)
            {
                CvInvoke.Rectangle(image, rect, new MCvScalar(0, 255, 0), 2);
            }

            // 5. Save the result to a new file
            CvInvoke.Imwrite("faces_detected.jpg", image);
        }
    }
}

