using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;
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

    public FontFamily ImageInfoFontFamily
    {
        get => _imageInfoFontFamily;
        set => SetProperty(ref _imageInfoFontFamily, value);
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value);
    }

    public ImageInfoManager(LabelManager labelManager)
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
                Trace.WriteLine($"MetadataLoaded[ImageInfoManager]: start version={md.Version}, infoLength={md.ImageInfo?.Length ?? 0}");

                BackgroundColor = Color.FromArgb(md.ImageInfoFont.Background);
                ImageInfoFontColor = Color.FromArgb(md.ImageInfoFont.Foreground);
                ImageInfoFontFamily = FontFamilyResolver.Resolve(md.ImageInfoFont.Family, ImageInfoFontFamily);

                var scale = md is AutoNumMetaData_V3 v3
                    ? v3.ImageInfoScale
                    : ResolveLegacyScale(md.ImageInfoFont.Size, md.LabelsFont.Size);

                FontScale = scale;
                ImageInfo = md.ImageInfo ?? string.Empty;
                IsEnabled = md.ImageInfoEnabled ?? !string.IsNullOrEmpty(md.ImageInfo);

                Trace.WriteLine("MetadataLoaded[ImageInfoManager]: completed");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"MetadataLoaded[ImageInfoManager]: failed - {ex}");
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

        ImageInfoFontSize = SizingModel.ResolveSize(baseLabelFontSize, FontScale);
    }

    private static double ResolveLegacyScale(double actualInfoFontSize, double legacyLabelFontSize)
    {
        return SizingModel.SafeScale(actualInfoFontSize, legacyLabelFontSize);
    }

    private readonly LabelManager _labelManager;
    private bool _isEnabled;
    private double _fontScale = 1.0;
    private string _imageInfo = string.Empty;
    private Color _fontColor = Color.Black;
    private double _fontSize = 1;
    private Color _backgroundColor = Color.White;
    private FontFamily _imageInfoFontFamily = new("Calibri");
}
