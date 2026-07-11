using AutoNumber.Model;

namespace AutoNumber.ViewModels;

public class TitleManager(LabelManager labelManager) : TextElementManagerBase(labelManager)
{
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    protected override AutoNumFont MetadataFont(AutoNumMetaData_V1 md) => md.TitleFont;

    protected override double MetadataScale(AutoNumMetaData_V3 v3) => v3.TitleScale;

    protected override void RestoreElementState(AutoNumMetaData_V1 md)
    {
        Title = md.Title ?? string.Empty;
        IsEnabled = md.TitleEnabled ?? !string.IsNullOrEmpty(md.Title);
    }

    private string _title = string.Empty;
}
