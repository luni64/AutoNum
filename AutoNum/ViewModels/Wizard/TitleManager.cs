using NumberIt.Model;
using System.Drawing;


namespace NumberIt.ViewModels
{
    public class TitleManager : BaseViewModel
    {
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                SetProperty(ref _isEnabled, value);              
            }
        }

        public double FontSizeSliderValue
        {
            get => _fontSizeSliderValue;
            set
            {
                var pvm = parent.pictureVM;
                if (_fontSizeSliderValue != value)
                {
                    _fontSizeSliderValue = value;
                    FontSize = DefaultFontSize * (0.5 + 0.0002 * (_fontSizeSliderValue * _fontSizeSliderValue));
                    OnPropertyChanged();
                }
            }
        }
        
        double _fontSize = 1;
        public double FontSize
        {
            get => _fontSize;
            private set => SetProperty(ref _fontSize, value);
        }

        Color _fontColor = Color.Black;
        public Color FontColor
        {
            get => _fontColor;
            set => SetProperty(ref _fontColor, value);
        }

        public FontFamily FontFamily { get; } = new FontFamily("Calibri");

        Color _backgroundColor = Color.White;
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }

        public static double DefaultFontSize = 80;

        public TitleManager(MainVM parent)
        {
            this.parent = parent;
            FontSizeSliderValue = 50;
        }

        MainVM parent { get; set; }
        bool _isEnabled = false;
        double _fontSizeSliderValue;
        string _title = string.Empty;
    }
}
