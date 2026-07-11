using AutoNumber.Model;

namespace AutoNumber.ViewModels;

public class ImageIdManager(LabelManager labelManager) : TextElementManagerBase(labelManager)
{
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

    public override bool IsEnabled
    {
        get => base.IsEnabled;
        set
        {
            base.IsEnabled = value;
            ApplyScale();
            OnPropertyChanged(nameof(ShowImageIdLine));
        }
    }

    public bool ShowImageIdLine => IsEnabled && !string.IsNullOrWhiteSpace(ImageId);

    /// <summary>Rendered height of the image-ID banner line (0 when hidden).</summary>
    public double LineHeight
    {
        get => _lineHeight;
        private set => SetProperty(ref _lineHeight, value);
    }

    protected override void OnAppearanceChanged()
    {
        LineHeight = ShowImageIdLine
            ? Analyzer.GetTextBlockHeight(ImageId, FontFamily, FontSize) + 10
            : 0;
    }

    protected override AutoNumFont MetadataFont(AutoNumMetaData_V1 md) => md.ImageIdFont;

    protected override double MetadataScale(AutoNumMetaData_V3 v3) => v3.ImageIdScale;

    // Legacy files may predate a stored image-ID font size; fall back to the names font.
    protected override double LegacyStoredFontSize(AutoNumMetaData_V1 md) =>
        md.ImageIdFont.Size > 0 ? md.ImageIdFont.Size : md.NamesFont.Size;

    protected override void RestoreElementState(AutoNumMetaData_V1 md)
    {
        using var _ = SuspendNotifications();
        ImageId = md.ImageId;
        IsEnabled = md.ImageIdEnabled ?? !string.IsNullOrWhiteSpace(md.ImageId);
    }

    private string _imageId = string.Empty;
    private double _lineHeight;
}
