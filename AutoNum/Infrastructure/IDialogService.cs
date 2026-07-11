namespace AutoNumber.Infrastructure
{
    /// <summary>Parameters for the native open/save file dialogs.</summary>
    public class FileDialogInfo
    {
        public string? Filename { get; set; }
        public string? InitialDirectory { get; set; }
        public string? Filter { get; set; }
        public int FilterIndex { get; set; }
    }

    public interface IDialogService
    {
        /// <summary>Native open-file dialog. Returns the chosen path, or null when cancelled.</summary>
        string? ShowOpenFileDialog(FileDialogInfo info);

        /// <summary>Native save-file dialog. Returns the chosen path, or null when cancelled.</summary>
        string? ShowSaveFileDialog(FileDialogInfo info);

        /// <summary>Modal error message box.</summary>
        void ShowError(string message);

        /// <summary>Modal warning message box (non-fatal, operation continues).</summary>
        void ShowWarning(string message);

        /// <summary>
        /// Modal prompt shown after writing a file failed (typically because another
        /// application holds it open). Returns true when the user chooses "Wiederholen",
        /// false when they cancel the save.
        /// </summary>
        bool ShowSaveRetryDialog(string operationName, string filename, string details);
    }
}
