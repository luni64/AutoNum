using AutoNumber.ViewModels;

namespace AutoNumber.Model;

/// <summary>
/// V3 metadata schema. Adds stable sizing anchors and relative scales so sizes can
/// be restored exactly while still allowing a normalized 100% baseline model.
/// </summary>
public class AutoNumMetaData_V3 : AutoNumMetaData_V2
{
    public double BaseLabelDiameter { get; set; }
    public double BaseLabelFontSize { get; set; }
    public double LabelScale { get; set; } = SizingModel.DefaultScale;
    public double NameScale { get; set; } = SizingModel.DefaultScale;
    public double ImageIdScale { get; set; } = SizingModel.DefaultScale;
    public double TitleScale { get; set; } = SizingModel.DefaultScale;
    public double ImageInfoScale { get; set; } = SizingModel.DefaultScale;

    public AutoNumMetaData_V3() : base()
    {
        Version = "V3";
    }

    public AutoNumMetaData_V3(ImageVM model, LabelManager lm, NameManager nm, TitleManager tm, ImageInfoManager iim, ImageIdManager idm)
        : base(model, lm, nm, tm, iim, idm)
    {
        Version = "V3";

        BaseLabelDiameter = double.IsFinite(lm.BaseLabelDiameter) && lm.BaseLabelDiameter > 0
            ? lm.BaseLabelDiameter
            : MarkerLabel.Style.Diameter;

        BaseLabelFontSize = double.IsFinite(lm.BaseLabelFontSize) && lm.BaseLabelFontSize > 0
            ? lm.BaseLabelFontSize
            : MarkerLabel.Style.FontSize;

        LabelScale = SizingModel.SafeScale(MarkerLabel.Style.Diameter, BaseLabelDiameter);
        NameScale = SizingModel.SafeScale(TextLabel.Style.FontSize, BaseLabelFontSize);
        ImageIdScale = SizingModel.SafeScale(idm.FontSize, BaseLabelFontSize);
        TitleScale = SizingModel.SafeScale(tm.TitleFontSize, BaseLabelFontSize);
        ImageInfoScale = SizingModel.SafeScale(iim.ImageInfoFontSize, BaseLabelFontSize);
    }
}
