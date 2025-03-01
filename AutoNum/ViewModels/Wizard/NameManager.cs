using Emgu.CV.ML;
using NumberIt.Model;
using System.ComponentModel;
using System.Drawing;

namespace NumberIt.ViewModels
{
    public class NameManager : WizardStep
    {
       // public ICollectionView Labels => parent.pictureVM.Labels;

        public MainVM parent { get; set; }

        bool _isEnabled = false;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                SetProperty(ref _isEnabled, value);
                ShowNames();
            }
        }

        public void ShowNames()
        {
            var pvm = parent.pictureVM;
            if (IsEnabled)
            {
                var height = Analyzer.PlacePersonNames(pvm.MarkerVMs.OfType<TextLabel>(), pvm.ImageWidth, pvm.ImageHeight);
                pvm.NamesRegionHeight = height;
            }
            else
            {
                foreach (TextLabel name in pvm.Names)
                {
                    name.visible = false;
                }
            }
        }

        private Color _fontColor = Color.Black;
        public Color FontColor
        {
            get => _fontColor;
            set
            {
                SetProperty(ref _fontColor, value);
                TextLabel.FontColor = value;
            }
        }

        private Color _backgroundColor = Color.White;
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }

        double _fontSizeSliderValue;
        public double FontSizeSliderValue
        {
            get => _fontSizeSliderValue;
            set
            {
                var pvm = parent.pictureVM;
                if (_fontSizeSliderValue != value)
                {
                    _fontSizeSliderValue = value;
                    TextLabel.FontSize = DefaultFontSize * (0.5 + 0.0002 * (_fontSizeSliderValue * _fontSizeSliderValue));                    
                    ShowNames();
                    OnPropertyChanged();
                }
            }
        }
        public static double DefaultFontSize = 80;

        public FontFamily FontFamily { get; } = new FontFamily("Calibri");
        
        public NameManager(MainVM parent)
        {
            this.parent = parent;
            FontSizeSliderValue = 50;
        }
    }
}
