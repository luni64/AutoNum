using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using System.Drawing;

namespace AutoNumber.ViewModels;

public class ImageIdManager : BaseViewModel
{
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            SetProperty(ref _isEnabled, value);
            ApplyScale();
            OnPropertyChanged(nameof(ShowImageIdLine));
        }
    }

    public string ImageId
    {
        get => _imageId;
        set
        {
            SetProperty(ref _imageId, value);
            ApplyScale();
            OnPropertyChanged(nameof(ShowImageIdLine));
        }
    }

    public bool ShowImageIdLine => IsEnabled && !string.IsNullOrWhiteSpace(ImageId);

    public Color FontColor
    {
        get => _fontColor;
        set => SetProperty(ref _fontColor, value);
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value);
    }

    public FontFamily FontFamily { get; } = new("Calibri");

    public double FontSize
    {
        get => _fontSize;
        private set => SetProperty(ref _fontSize, value);
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

    public double LineHeight
    {
        get => _lineHeight;
        private set => SetProperty(ref _lineHeight, value);
    }

    public ImageIdManager(LabelManager labelManager)
    {
        _labelManager = labelManager;
        FontScale = 1.0;

        WeakReferenceMessenger.Default.Register<LabelsChangedMessage>(this, (r, msg) =>
        {
            ApplyScale();
        });

        WeakReferenceMessenger.Default.Register<MetadataLoadedMessage>(this, (r, msg) =>
        {
            var md = msg.Metadata;
            ImageId = md.ImageId;
            IsEnabled = md.ImageIdEnabled ?? !string.IsNullOrWhiteSpace(md.ImageId);
            BackgroundColor = Color.FromArgb(md.ImageIdFont.Background);
            FontColor = Color.FromArgb(md.ImageIdFont.Foreground);

            var scale = md is AutoNumMetaData_V3 v3
                ? v3.ImageIdScale
                : ResolveLegacyScale(md.ImageIdFont.Size > 0 ? md.ImageIdFont.Size : md.NamesFont.Size, md.LabelsFont.Size);

            FontScale = scale;
        });
    }

    private void ApplyScale()
    {
        var baseLabelFontSize = _labelManager.BaseLabelFontSize;
        if (baseLabelFontSize <= 0)
        {
            return;
        }

        FontSize = SizingModel.ResolveSize(baseLabelFontSize, FontScale);
        LineHeight = ShowImageIdLine
            ? Analyzer.GetTextBlockHeight(ImageId, FontFamily, FontSize) + 10
            : 0;
    }

    private static double ResolveLegacyScale(double actualIdFontSize, double legacyLabelFontSize)
        => SizingModel.SafeScale(actualIdFontSize, legacyLabelFontSize);

    private readonly LabelManager _labelManager;
    private bool _isEnabled;
    private string _imageId = string.Empty;
    private Color _fontColor = Color.Black;
    private Color _backgroundColor = Color.White;
    private double _fontSize = 1;
    private double _fontScale = 1.0;
    private double _lineHeight;
}
