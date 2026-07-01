using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;
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

        /// <summary>
        /// Font scale factor (0.25–4.0). Model property that drives actual font size.
        /// </summary>
        public double FontScale
        {
            get => _fontScale;
            set
            {
                var clamped = Math.Clamp(value, 0.25, 4.0);
                if (_fontScale != clamped)
                {
                    _fontScale = clamped;
                    ApplyScale();
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
        public FontFamily TitleFontFamily
        {
            get => _titleFontFamily;
            set => SetProperty(ref _titleFontFamily, value);
        }

        public Color BackgroundColor
        {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }

        public TitleManager(LabelManager labelManager)
        {
            _labelManager = labelManager;
            FontScale = 1.0;

            WeakReferenceMessenger.Default.Register<LabelsChangedMessage>(this, (r, msg) =>
            {
                ApplyScale();
            });

            WeakReferenceMessenger.Default.Register<MetadataLoadedMessage>(this, (r, msg) =>
            {
                try
                {
                    var md = msg.Metadata;
                    Trace.WriteLine($"MetadataLoaded[TitleManager]: start version={md.Version}, titleLength={md.Title?.Length ?? 0}");

                    BackgroundColor = Color.FromArgb(md.TitleFont.Background);
                    TitleFontColor = Color.FromArgb(md.TitleFont.Foreground);
                    TitleFontFamily = FontFamilyResolver.Resolve(md.TitleFont.Family, TitleFontFamily);

                    var scale = md is AutoNumMetaData_V3 v3
                        ? v3.TitleScale
                        : ResolveLegacyScale(md.TitleFont.Size, md.LabelsFont.Size);

                    FontScale = scale;
                    Title = md.Title ?? string.Empty;
                    IsEnabled = md.TitleEnabled ?? !string.IsNullOrEmpty(md.Title);

                    Trace.WriteLine("MetadataLoaded[TitleManager]: completed");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"MetadataLoaded[TitleManager]: failed - {ex}");
                    throw;
                }
            });
        }

        private void ApplyScale()
        {
            var baseLabelFontSize = _labelManager.BaseLabelFontSize;
            if (baseLabelFontSize <= 0)
            {
                return;
            }

            TitleFontSize = SizingModel.ResolveSize(baseLabelFontSize, FontScale);
        }

        private static double ResolveLegacyScale(double actualTitleFontSize, double legacyLabelFontSize)
        {
            return SizingModel.SafeScale(actualTitleFontSize, legacyLabelFontSize);
        }

        private readonly LabelManager _labelManager;

        bool _isEnabled = false;
        double _fontScale = 1.0;
        string _title = string.Empty;
        Color _fontColor = Color.Black;
        double _fontSize = 1;
        Color _backgroundColor = Color.White;
        FontFamily _titleFontFamily = new("Calibri");
    }
}
