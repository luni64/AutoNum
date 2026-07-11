using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AutoNumber.Infrastructure
{
    internal class DialogService : IDialogService
    {
        public string? ShowOpenFileDialog(FileDialogInfo info)
        {
            var dialog = new OpenFileDialog
            {
                Filter = info.Filter,
                FilterIndex = info.FilterIndex,
                InitialDirectory = info.InitialDirectory,
                CheckFileExists = true,
                ForcePreviewPane = true,
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowSaveFileDialog(FileDialogInfo info)
        {
            var dialog = new SaveFileDialog
            {
                FileName = info.Filename,
                InitialDirectory = info.InitialDirectory,
                Filter = info.Filter,
                FilterIndex = info.FilterIndex,
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message)
        {
            MessageBox.Show(message, "Hinweis", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public bool ShowSaveRetryDialog(string operationName, string filename, string details)
        {
            var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
                ?? Application.Current?.MainWindow;

            var dialog = new Window
            {
                Title = "Speichern fehlgeschlagen",
                Width = 560,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Owner = owner,
            };

            var grid = new Grid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var text = new TextBlock
            {
                Text = $"Die {operationName}-Datei konnte nicht gespeichert werden.\n\n" +
                       $"Möglicherweise ist die Datei in einer anderen Anwendung geöffnet.\n\n" +
                       $"Datei: {Path.GetFileName(filename)}\n" +
                       $"Details: {details}\n\n" +
                       "Schließen Sie die Datei in der anderen Anwendung und klicken Sie auf \"Wiederholen\", oder brechen Sie den Vorgang ab.",
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(text, 0);
            grid.Children.Add(text);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var retryButton = new Button
            {
                Content = "Wiederholen",
                Width = 110,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "Abbrechen",
                Width = 110,
                IsCancel = true
            };

            bool? retrySelected = null;
            retryButton.Click += (_, __) => { retrySelected = true; dialog.DialogResult = true; dialog.Close(); };
            cancelButton.Click += (_, __) => { retrySelected = false; dialog.DialogResult = false; dialog.Close(); };

            buttons.Children.Add(retryButton);
            buttons.Children.Add(cancelButton);
            Grid.SetRow(buttons, 1);
            grid.Children.Add(buttons);

            dialog.Content = grid;
            dialog.ShowDialog();
            return retrySelected == true;
        }
    }
}
