using Emgu.CV.ML;
using NumberIt.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.AccessControl;

namespace NumberIt.ViewModels
{
    public class NameManager : WizardStep
    {
        public ICollectionView Labels => parent.pictureVM.Labels;

        public MainVM parent { get; set; }

        bool _isEnabled = false;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
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
                    Analyzer.PlacePersonNames(pvm.MarkerVMs.OfType<TextLabel>(), pvm.ImageWidth, pvm.ImageHeight);
                    OnPropertyChanged();
                }
            }
        }
        public static double DefaultFontSize = 80;

        public NameManager(MainVM parent)
        {
            this.parent = parent;
            FontSizeSliderValue = 50;
        }
    }
}
