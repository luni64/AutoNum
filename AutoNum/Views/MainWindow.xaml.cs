using MahApps.Metro.Controls;
using AutoNumber.ViewModels;
using AutoNumber.Views;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;

namespace AutoNumber
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
      
        public MainWindow(MainVM mainVM)
        {
            InitializeComponent();
            this.DataContext = mainVM;
          mainVM.DialogCoordinator  =   DialogCoordinator.Instance;
        }

        private void Border_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(DataContext is MainVM mainVM)
            {
                mainVM.PictureVM.CanvasSize = new System.Drawing.Size((int)e.NewSize.Width, (int)e.NewSize.Height);
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainVM mainVM)
            {
                return;
            }

            var settingsWindow = new SettingsWindow(mainVM.SettingsManager)
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }
    }
}