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

    public AutoNumMetaData_V4(ImageVM model, LabelManager lm, NameManager nm, TitleManager tm, ImageInfoManager iim, ImageIdManager idm)
        : base(model, lm, nm, tm, iim, idm)
    {
        Version = "V4";

        var session = model.RowDefinitionSession;
        if (session is not null)
        {
            RowCount = Math.Max(1, session.RowCount);
            RowBoundaries = session.Boundaries
                .Select(boundary => new RowBoundary(boundary.LeftY, boundary.RightY))
                .ToList();
        }
        else
        {
            // If no active session, try to get saved state from memory
            var (savedRowCount, savedBoundaries) = model.GetSavedRowDefinitionState();
            if (savedBoundaries.Count > 0)
            {
                RowCount = Math.Max(1, savedRowCount);
                RowBoundaries = savedBoundaries
                    .Select(boundary => new RowBoundary(boundary.LeftY, boundary.RightY))
                    .ToList();
            }
            else
            {
                RowCount = 1;
            }
        }
    }
}
