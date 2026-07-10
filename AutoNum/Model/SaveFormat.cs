namespace AutoNumber.Model;

/// <summary>
/// Which format is preselected in the "Speichern unter..." dialog.
/// The actual format used when saving is always decided by the extension
/// of the path the user picks/types in that dialog, not by this setting.
/// </summary>
public enum SaveFormat
{
    Jpg,
    Pdf,
}
