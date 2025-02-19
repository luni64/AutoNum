using Emgu.CV;
using System.Collections.ObjectModel;
using System.Drawing;


namespace NumberIt.ViewModels
{
    public class ImageModel : BaseViewModel, IDisposable
    {
        #region Properties ----------------------------------------------------
        public ObservableCollection<MarkerVM> MarkerVMs { get; } = [];
        public Mat? emguImage
        {
            get => _emguBitmap;
            set
            {
                if (_emguBitmap != value)
                {
                    _emguBitmap?.Dispose();
                    _emguBitmap = value;
                    OnPropertyChanged();                   
                }
            }
        }
        public String Filename { get; set; } = "";

        public Size CanvasSize { get; set; }

        public int ImageWidth
        {
            get => _imageWidth;
            set => SetProperty(ref _imageWidth, value);
        }
        public int ImageHeight
        {
            get => _imageHeight;
            set => SetProperty(ref _imageHeight, value);
        }
        public int PanX
        {
            get => _panX;
            set => SetProperty(ref _panX, value);
        }
        public int PanY
        {
            get => _panY;
            set => SetProperty(ref _panY, value);
        }
        public double Zoom
        {
            get => _zoom;
            set => SetProperty(ref _zoom, value);
        }

        #endregion

        public void Init(string imageFilename)
        {
            emguImage = CvInvoke.Imread(imageFilename, Emgu.CV.CvEnum.ImreadModes.Color);
            if (!emguImage.IsEmpty)
            {                
                Filename = imageFilename;

                MarkerVMs.Clear();

                ImageWidth = emguImage.Width;
                ImageHeight = emguImage.Height;

                Zoom = 0.95 * Math.Min((double)CanvasSize.Width / ImageWidth, (double)CanvasSize.Height / ImageHeight);
                PanX = (int)((CanvasSize.Width - ImageWidth * Zoom) / 2);
                PanY = (int)((CanvasSize.Height - ImageHeight * Zoom) / 2);
            }
        }
        public void Dispose() => emguImage?.Dispose();

        int _imageHeight, _imageWidth;
        int _panX, _panY;
        double _zoom = 0.7;        
        private Mat? _emguBitmap;
    }
}
