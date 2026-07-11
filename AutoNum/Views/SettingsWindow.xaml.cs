using AutoNumber.ViewModels;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.IO;
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

    private void BrowseOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsManager settingsManager)
        {
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = "Ausgabeordner auswählen",
            InitialDirectory = Directory.Exists(settingsManager.OutputFolder) ? settingsManager.OutputFolder : null,
        };

        if (dialog.ShowDialog(this) == true)
        {
            settingsManager.OutputFolder = dialog.FolderName;
        }
    }
}
