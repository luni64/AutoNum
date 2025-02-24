﻿using Emgu.CV.Text;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;

namespace NumberIt.ViewModels
{
    class MarkerRect : MarkerVM
    {
        //public double X { get => _x; set => SetProperty(ref _x, value); }
        //public double Y { get => _y; set => SetProperty(ref _y, value); }

        //private double _x, _y, _w, _h;
    }

    public class MarkerLabel : MarkerVM
    {
        #region properties ------------------------------------------------------
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
            set
            {
                CenterY = value + H / 2;
                OnPropertyChanged(nameof(Y));
            }
        }

        public string Number
        {
            get => _nr;
            set => SetProperty(ref _nr, value);
        }
        public string? Name
        {
            get => _name == "" ? null : _name;
            set => SetProperty(ref _name, value);
        }

        public double CenterX
        {
            get => _centerX;
            set => SetProperty(ref _centerX, value);
        }

        public double CenterY
        {
            get => _centerY;
            set => SetProperty(ref _centerY, value);
        }        

        public static float Diameter
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

        public static double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    //BaselineOffset = 0.04 * FontSize;

                    OnStaticPropertyChanged(nameof(FontSize));

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

        #endregion

        #region events --------------------------------------------------------
        public static event EventHandler<PropertyChangedEventArgs>? StaticPropertyChanged;
        protected static void OnStaticPropertyChanged(string name)
        {
            var handler = StaticPropertyChanged;

            if (handler != null)
            {
                handler(null, new PropertyChangedEventArgs(name));
            }
        }
        #endregion

        #region private fields -----------------------------------------------
        private static Color _edgeColor = Color.White;
        private static Color _backgroundColor = Color.Green;
        private static Color _fontColor = Color.Black;
        private static double _fontSize;

        private string _nr = string.Empty;
        private string? _name;
        private double _centerX;
        private double _centerY;
        private static float _diameter;

        #endregion
    }
}
