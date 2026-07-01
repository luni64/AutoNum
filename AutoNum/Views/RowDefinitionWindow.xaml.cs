using AutoNumber.ViewModels;
using MahApps.Metro.Controls;
using System.Windows;

namespace AutoNumber.Views;

public partial class RowDefinitionWindow : MetroWindow
{
    public RowDefinitionWindow(RowDefinitionManager manager)
    {
        InitializeComponent();
        DataContext = manager;
        Loaded += RowDefinitionWindow_Loaded;
        Closing += RowDefinitionWindow_Closing;
    }

    private void RowDefinitionWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RowDefinitionManager manager)
        {
            manager.Preview();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RowDefinitionWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is RowDefinitionManager manager)
        {
            manager.CloseDialog();
        }
    }
}
