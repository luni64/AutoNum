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
                var clamped = Math.Clamp(value, SizingModel.SliderPercentMin, SizingModel.SliderPercentMax);
                var changed = _fontSizeSliderValue != clamped;
                _fontSizeSliderValue = clamped;
                ApplyScaleFromSlider();
                if (changed)
                {
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

        public TitleManager(LabelManager labelManager)
        {
            _labelManager = labelManager;
            FontSizeSliderValue = SizingModel.SliderPercentDefault;

            WeakReferenceMessenger.Default.Register<LabelsChangedMessage>(this, (r, msg) =>
            {
                ApplyScaleFromSlider();
            });

            WeakReferenceMessenger.Default.Register<MetadataLoadedMessage>(this, (r, msg) =>
            {
                var md = msg.Metadata;
                BackgroundColor = Color.FromArgb(md.TitleFont.Background);
                TitleFontColor = Color.FromArgb(md.TitleFont.Foreground);

                var scale = md is AutoNumMetaData_V3 v3
                    ? v3.TitleScale
                    : ResolveLegacyScale(md.TitleFont.Size, md.LabelsFont.Size);

                FontSizeSliderValue = SizingModel.ScaleToSliderPercent(scale);
                Title = md.Title;
                IsEnabled = md.TitleEnabled ?? !string.IsNullOrEmpty(md.Title);
            });
        }

        private void ApplyScaleFromSlider()
        {
            var baseLabelFontSize = _labelManager.BaseLabelFontSize;
            if (baseLabelFontSize <= 0)
            {
                return;
            }

            TitleFontSize = SizingModel.ResolveSize(baseLabelFontSize, SizingModel.SliderPercentToScale(FontSizeSliderValue));
        }

        private static double ResolveLegacyScale(double actualTitleFontSize, double legacyLabelFontSize)
        {
            return SizingModel.SafeScale(actualTitleFontSize, legacyLabelFontSize);
        }

        private readonly LabelManager _labelManager;

        bool _isEnabled = false;
        double _fontSizeSliderValue = SizingModel.SliderPercentDefault;
        string _title = string.Empty;
        Color _fontColor = Color.Black;
        double _fontSize = 1;
        Color _backgroundColor = Color.White;
    }
}
