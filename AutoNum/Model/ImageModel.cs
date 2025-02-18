using Emgu.CV;
using System.Collections.ObjectModel;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace NumberIt.ViewModels

{
    public class ImageModel : BaseViewModel, IDisposable
    {
        public ObservableCollection<MarkerVM> MarkerVMs { get; } = [];
        public Mat? emguImage
        {
            get => _emguBitmap;
            set
            {
                if (_emguBitmap != value)
                {
                    _emguBitmap?.Dispose();
                    SetProperty(ref _emguBitmap, value);
                }
            }
        }
        public BitmapSource? ImageSource
        {
            get => _imageSource;
            set
            {
                SetProperty(ref _imageSource, value);

                ImageWidth = _imageSource?.PixelWidth ?? 0;
                ImageHeight = _imageSource?.PixelHeight ?? 0;
                Zoom = 0.95 * Math.Min(CanvasSize.Width / ImageWidth, CanvasSize.Height / ImageHeight);

                PanX =(int)( (CanvasSize.Width - ImageWidth * Zoom) / 2);
                PanY = (int)((CanvasSize.Height - ImageHeight * Zoom) / 2);
                

                //PanX = (int)(ImageWidth * Zoom * 0.025);
                //PanY = PanX;
            }
        }

        public Size CanvasSize { get; set; }


        public String Filename { get; set; } = "";

        public int ImageWidth //{ get; set; }
        {
            get => _imageWidth;
            set => SetProperty(ref _imageWidth, value);
        }

        int _imageHeight;
        public int ImageHeight //{ get; set; }
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

        public void Dispose() => emguImage?.Dispose();

        int _imageWidth;
        int _panX, _panY;
        double _zoom = 0.7;
        BitmapSource? _imageSource;
        private Mat? _emguBitmap;
    }
}
