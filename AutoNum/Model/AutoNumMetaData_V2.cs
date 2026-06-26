using AutoNumber.ViewModels;

namespace AutoNumber.Model;

/// <summary>
/// V2 metadata schema. Adds original image dimensions so the base image can be
/// reconstructed from embedded patches without needing the original file.
/// </summary>
public class AutoNumMetaData_V2 : AutoNumMetaData_V1
{
    public int OriginalImageWidth { get; set; }
    public int OriginalImageHeight { get; set; }
    public int TitleHeight { get; set; }

    public AutoNumMetaData_V2() : base()
    {
        Version = "V2";
    }

    public AutoNumMetaData_V2(ImageVM model, LabelManager lm, NameManager nm, TitleManager tm, ImageInfoManager iim, ImageIdManager idm)
        : base(model, lm, nm, tm, iim, idm)
    {
        Version = "V2";
        OriginalImageWidth = model.Bitmap?.Width ?? 0;
        OriginalImageHeight = model.Bitmap?.Height ?? 0;

        bool hasTopText = (tm.IsEnabled && !string.IsNullOrEmpty(tm.Title)) || (iim.IsEnabled && !string.IsNullOrEmpty(iim.ImageInfo));
        TitleHeight = hasTopText ? (int)model.TitleRegionHeight : 0;
    }
}
