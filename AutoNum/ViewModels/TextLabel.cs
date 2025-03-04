using System.ComponentModel;
using System.Drawing;

namespace AutoNumber.ViewModels
{
    public class TextLabel : MarkerVM
    {
        #region properties ------------------------------------------------------
        //public string Number
        //{
        //    get => _nr;
        //    set
        //    {
        //        SetProperty(ref _nr, value);
        //        OnPropertyChanged("FullName");
        //    }
        //}
        public string? Text
        {
            get => _text == "" ? null : _text;
            set
            {
                SetProperty(ref _text, value);
                person.OnPropertyChanged("FullName");
            }
        }

                     

        public static FontFamily fontFamily { get; } = new FontFamily("Calibri");
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
        public static double FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    //BaselineOffset = 0.04 * TitleFontSize;

                    OnStaticPropertyChanged(nameof(FontSize));

                }
            }
        }
        #endregion

        #region events --------------------------------------------------------
        public static event EventHandler<PropertyChangedEventArgs>? StaticPropertyChanged;
        private static void OnStaticPropertyChanged(string name) //=> StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(name));
        {
            var handler = StaticPropertyChanged;
            if (handler != null)
            {
                handler(null, new PropertyChangedEventArgs(name));
            }
        }
        #endregion
        public override string ToString()
        {
            return $"TL {Text}";
        }

        public TextLabel(Person person)
        {
            this.person = person;
        }

        public Person person { get; }

        #region private fields -----------------------------------------------
        private static Color _fontColor = Color.Black;
        private static double _fontSize;// = _defaultFontSize;

        private string _nr = string.Empty;
        private string? _text;       

        #endregion
    }
}