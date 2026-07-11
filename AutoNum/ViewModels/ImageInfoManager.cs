using AutoNumber.Model;

namespace AutoNumber.ViewModels;

public class ImageInfoManager(LabelManager labelManager) : TextElementManagerBase(labelManager)
{
    public string ImageInfo
    {
        get => _imageInfo;
        set => SetProperty(ref _imageInfo, value);
    }

    protected override AutoNumFont MetadataFont(AutoNumMetaData_V1 md) => md.ImageInfoFont;

    protected override double MetadataScale(AutoNumMetaData_V3 v3) => v3.ImageInfoScale;

    protected override void RestoreElementState(AutoNumMetaData_V1 md)
    {
        ImageInfo = md.ImageInfo ?? string.Empty;
        IsEnabled = md.ImageInfoEnabled ?? !string.IsNullOrEmpty(md.ImageInfo);
    }

    private string _imageInfo = string.Empty;
}
