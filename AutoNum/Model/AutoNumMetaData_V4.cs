using AutoNumber.ViewModels;

namespace AutoNumber.Model;

/// <summary>
/// V4 metadata schema. Adds persisted row-definition helper boundaries so users can refine rows later.
/// </summary>
public class AutoNumMetaData_V4 : AutoNumMetaData_V3
{
    public int RowCount { get; set; } = 1;
    public List<RowBoundary> RowBoundaries { get; set; } = [];

    public AutoNumMetaData_V4() : base()
    {
        Version = "V4";
    }

    // Note: Constructor with parameters is no longer needed.
    // Runtime state is now maintained in the persistent CurrentMetadata instance
    // and updated via ImageVM.UpdateMetadataBeforeSave() before saving.
}
