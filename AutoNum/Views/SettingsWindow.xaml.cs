using AutoNumber.ViewModels;
using MahApps.Metro.Controls;
using System.Windows;

namespace AutoNumber.Views;

public partial class SettingsWindow : MetroWindow
{
    private readonly MainVM _mainVM;

    public SettingsWindow(SettingsManager settingsManager, MainVM mainVM)
    {
        _mainVM = mainVM;
        InitializeComponent();
        DataContext = settingsManager;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsManager settingsManager)
        {
            // Apply formatting defaults to the current image without changing visibility toggles.
            settingsManager.ApplyCurrentImageFormattingDefaults(
                _mainVM.LabelManager,
                _mainVM.NameManager,
                _mainVM.TitleManager,
                _mainVM.ImageInfoManager,
                _mainVM.ImageIdManager);
        }
    }

    private void ApplyVisibility_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsManager settingsManager)
        {
            settingsManager.ApplyCurrentImageVisibilityDefaults(
                _mainVM.NameManager,
                _mainVM.TitleManager,
                _mainVM.ImageInfoManager,
                _mainVM.ImageIdManager);
        }
    }

    private void ExportNow_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsManager)
        {
            var exportedFiles = _mainVM.FileManager.ExportMetadataNow();
            if (exportedFiles.Count > 0)
            {
                var message = "Folgende Dateien wurden exportiert:\n\n" + string.Join("\n", exportedFiles);
                MessageBox.Show(this, message, "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
