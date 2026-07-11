using AutoNumber.ViewModels;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace AutoNumber.Views;

public partial class SettingsWindow : MetroWindow
{
    private readonly MainVM _mainVM;
    private bool _committed;

    public SettingsWindow(SettingsManager settingsManager, MainVM mainVM)
    {
        _mainVM = mainVM;
        InitializeComponent();
        DataContext = settingsManager;

        // The dialog edits the live SettingsManager (so the "Anwenden" buttons see the edited
        // values), inside an edit transaction: OK commits + saves once, everything else
        // (Abbrechen, ✕, Alt+F4) restores the snapshot and writes nothing.
        settingsManager.BeginEdit();
        Closing += (_, _) =>
        {
            if (!_committed)
            {
                settingsManager.CancelEdit();
            }
        };
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsManager settingsManager)
        {
            settingsManager.CommitEdit();
            _committed = true;
        }

        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // The Closing handler performs the actual CancelEdit rollback.
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
