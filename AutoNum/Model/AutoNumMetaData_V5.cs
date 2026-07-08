namespace AutoNumber.Model;

/// <summary>
/// V5 metadata schema. Pure version marker: no new fields. Marks that
/// Label.CenterX/CenterY are the label circle's true center, rather than the
/// pre-V5 top-left-corner convention. Files saved as V1-V4 need their stored
/// positions migrated on load (see ImageVM.InitFromMetadata).
/// </summary>
public class AutoNumMetaData_V5 : AutoNumMetaData_V4
{
    public AutoNumMetaData_V5() : base()
    {
        Version = "V5";
    }
}
