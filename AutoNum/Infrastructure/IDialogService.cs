namespace AutoNumber.Infrastructure
{
    public interface IDialogService
    {
        object? ShowDialog(object viewModel);

        /// <summary>
        /// Modal prompt shown after writing a file failed (typically because another
        /// application holds it open). Returns true when the user chooses "Wiederholen",
        /// false when they cancel the save.
        /// </summary>
        bool ShowSaveRetryDialog(string operationName, string filename, string details);
    }
}
