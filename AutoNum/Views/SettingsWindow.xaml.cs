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
            // Apply defaults to the managers on the main screen
            if (_mainVM.LabelManager != null)
                settingsManager.ApplyFreshImageDefaults(
                    _mainVM.LabelManager,
                    _mainVM.NameManager,
                    _mainVM.TitleManager,
                    _mainVM.ImageInfoManager,
                    _mainVM.ImageIdManager);
        }
    }
}
