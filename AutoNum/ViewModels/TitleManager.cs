using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using System.Drawing;


namespace AutoNumber.ViewModels
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
                if (_fontSizeSliderValue != value)
                {
                    _fontSizeSliderValue = value;
                    TitleFontSize = DefaultFontSize * (0.5 + 0.0002 * (_fontSizeSliderValue * _fontSizeSliderValue));
                    OnPropertyChanged();
                }
            }
        }

        public double TitleFontSize
        {
            get => _fontSize;
            private set => SetProperty(ref _fontSize, value);
        }

        public Color TitleFontColor
        {
            get => _fontColor;
            set => SetProperty(ref _fontColor, value);
        }
        public FontFamily TitleFontFamily { get; } = new FontFamily("Calibri");

        public Color BackgroundColor
        {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }

        public static double DefaultFontSize = 80;

        public TitleManager()
        {
            FontSizeSliderValue = 50;

            WeakReferenceMessenger.Default.Register<MetadataLoadedMessage>(this, (r, msg) =>
            {
                var md = msg.Metadata;
                BackgroundColor = Color.FromArgb(md.TitleFont.Background);
                TitleFontColor = Color.FromArgb(md.TitleFont.Foreground);
                Title = md.Title;
                if (!string.IsNullOrEmpty(md.Title)) IsEnabled = true;
            });
        }

        bool _isEnabled = false;
        double _fontSizeSliderValue;
        string _title = string.Empty;
        Color _fontColor = Color.Black;
        double _fontSize = 1;
        Color _backgroundColor = Color.White;
    }
}
