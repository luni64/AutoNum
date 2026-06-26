using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using System.Drawing;

namespace AutoNumber.ViewModels;

public class ImageInfoManager : BaseViewModel
{
    public string ImageInfo
    {
        get => _imageInfo;
        set => SetProperty(ref _imageInfo, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
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

    public double ImageInfoFontSize
    {
        get => _fontSize;
        private set => SetProperty(ref _fontSize, value);
    }

    public Color ImageInfoFontColor
    {
        get => _fontColor;
        set => SetProperty(ref _fontColor, value);
    }

    public FontFamily ImageInfoFontFamily { get; } = new("Calibri");

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value);
    }

    public ImageInfoManager(LabelManager labelManager)
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
            BackgroundColor = Color.FromArgb(md.ImageInfoFont.Background);
            ImageInfoFontColor = Color.FromArgb(md.ImageInfoFont.Foreground);

            var scale = md is AutoNumMetaData_V3 v3
                ? v3.ImageInfoScale
                : ResolveLegacyScale(md.ImageInfoFont.Size, md.LabelsFont.Size);

            FontSizeSliderValue = SizingModel.ScaleToSliderPercent(scale);
            ImageInfo = md.ImageInfo;
            IsEnabled = md.ImageInfoEnabled ?? !string.IsNullOrEmpty(md.ImageInfo);
        });
    }

    private void ApplyScaleFromSlider()
    {
        var baseLabelFontSize = _labelManager.BaseLabelFontSize;
        if (baseLabelFontSize <= 0)
        {
            return;
        }

        ImageInfoFontSize = SizingModel.ResolveSize(baseLabelFontSize, SizingModel.SliderPercentToScale(FontSizeSliderValue));
    }

    private static double ResolveLegacyScale(double actualInfoFontSize, double legacyLabelFontSize)
    {
        return SizingModel.SafeScale(actualInfoFontSize, legacyLabelFontSize);
    }

    private readonly LabelManager _labelManager;
    private bool _isEnabled;
    private double _fontSizeSliderValue = SizingModel.SliderPercentDefault;
    private string _imageInfo = string.Empty;
    private Color _fontColor = Color.Black;
    private double _fontSize = 1;
    private Color _backgroundColor = Color.White;
}
