using Emgu.CV;
using System.Collections.ObjectModel;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace NumberIt.ViewModels
{
    public class ImageVM : BaseViewModel, IDisposable
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
                Zoom = 700.0 / ImageWidth;
            }
        }
        public String Filename = "";

        public int ImageWidth
        {
            get => _imageWidth;
            set => SetProperty(ref _imageWidth, value);
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
