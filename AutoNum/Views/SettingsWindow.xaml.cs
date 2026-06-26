using AutoNumber.ViewModels;
using MahApps.Metro.Controls;
using System.Windows;

namespace AutoNumber.Views;

public partial class SettingsWindow : MetroWindow
{
    public SettingsWindow(SettingsManager settingsManager)
    {
        InitializeComponent();
        DataContext = settingsManager;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
