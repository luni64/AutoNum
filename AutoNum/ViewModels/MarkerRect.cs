using Emgu.CV.Text;
using System.ComponentModel;
using System.Windows.Media;

namespace NumberIt.ViewModels
{
    class MarkerRect : MarkerVM
    {
        //public double X { get => _x; set => SetProperty(ref _x, value); }
        //public double Y { get => _y; set => SetProperty(ref _y, value); }

        //private double _x, _y, _w, _h;
    }

    class MarkerLabel : MarkerVM
    {
        override public double X
        {
            get => CenterX + W / 2;
            set
            {
                CenterX = value - W / 2;
                OnPropertyChanged(nameof(X));
            }

        }
        override public double Y
        {
            get => CenterY + H / 2;
            set { 
                CenterY = value + H / 2;
                OnPropertyChanged(nameof(Y));
            }
        }

        public string Number
        {
            get => _nr;
            set => SetProperty(ref _nr, value);
        }

        private double _centerX;
        public double CenterX
        {
            get => _centerX;
            set => SetProperty(ref _centerX, value);
        }

        private double _centerY;
        public double CenterY
        {
            get => _centerY;
            set => SetProperty(ref _centerY, value);
        }

        private static double _diameter;
        public static double Diameter
        {
            get => _diameter;
            set
            {
                if (_diameter != value)
                {
                    _diameter = value;
                    OnStaticPropertyChanged(nameof(Diameter));
                }
            }
        }


        public static Brush ForegroundBrush
        {
            get { return _foregroundBrush; }
            set
            {
                if (value != _foregroundBrush)
                {
                    _foregroundBrush = value;
                    OnStaticPropertyChanged(nameof(ForegroundBrush));
                }
            }
        }


        public static Brush EdgeBrush
        {
            get { return _edgeBrush; }
            set
            {
                if (_edgeBrush != value)
                {
                    _edgeBrush = value;
                    OnStaticPropertyChanged(nameof(EdgeBrush));
                }
            }
        }

        public static Brush BackgroundBrush
        {
            get { return _backgroundBrush; }
            set
            {
                if (value != _backgroundBrush)
                {
                    _backgroundBrush = value;
                    OnStaticPropertyChanged(nameof(BackgroundBrush));
                }
            }
        }




        private static Brush _edgeBrush = Brushes.White;
        private static Brush _backgroundBrush = Brushes.Black;
        private static Brush _foregroundBrush = Brushes.Black;

        private string _nr = "";

        public static event EventHandler<PropertyChangedEventArgs>? StaticPropertyChanged;
        protected static void OnStaticPropertyChanged(string name)
        {
            var handler = StaticPropertyChanged;

            if (handler != null)
            {
                handler(null, new PropertyChangedEventArgs(name));
            }
        }
    }
}
