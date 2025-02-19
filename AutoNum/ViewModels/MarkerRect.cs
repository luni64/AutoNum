using Emgu.CV.Text;
using System.ComponentModel;
using System.Drawing;

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

                
        public static Color FontColor
        {
            get => _fontColor;
            set
            {
                if (_fontColor != value)
                {
                    _fontColor = value;
                    OnStaticPropertyChanged(nameof(FontColor));
                }
            }
        }

        public static Color EdgeColor
        {
            get { return _edgeColor; }
            set
            {
                if (_edgeColor != value)
                {
                    _edgeColor = value;
                    OnStaticPropertyChanged(nameof(EdgeColor));
                }
            }
        }

        public static Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (value != _backgroundColor)
                {
                    _backgroundColor = value;
                    OnStaticPropertyChanged(nameof(BackgroundColor));
                }
            }
        }

        private static Color _edgeColor = Color.White;
        private static Color _backgroundColor = Color.Green;
        private static Color _fontColor = Color.Black;
        
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
