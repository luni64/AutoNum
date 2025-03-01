using System.ComponentModel;
using System.Drawing;
using System.Xml.Linq;

namespace NumberIt.ViewModels
{
    public class TextLabel : MarkerVM
    {
        #region properties ------------------------------------------------------
        public string Number
        {
            get => _nr;
            set => SetProperty(ref _nr, value);
        }
        public string? Text
        {
            get => _text == "" ? null : _text;
            set
            {
                SetProperty(ref _text, value);
                
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


        public static FontFamily fontFamily { get; } = new FontFamily("Calibri");


        //public static double _defaultFontSize;
        //public static double DefaultFontSize //{ get; set; }
        //{
        //    get => _defaultFontSize;
        //    set
        //    {
        //        if (value != _defaultFontSize)
        //        {
        //            _defaultFontSize = value;
        //            OnStaticPropertyChanged(nameof(DefaultFontSize));
        //            OnStaticPropertyChanged(nameof(FontSize));
        //        }

        //    }
        //}

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


        #endregion


        public override string ToString()
        {
            return $"TL: {Number}-{Text}";
        }


        #region events --------------------------------------------------------
        public static event EventHandler<PropertyChangedEventArgs>? StaticPropertyChanged;
        protected static void OnStaticPropertyChanged(string name) //=> StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(name));
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
        private static double _fontSize;// = _defaultFontSize;

        private string _nr = string.Empty;
        private string? _text;
        private static float _diameter;

        #endregion
    }
}