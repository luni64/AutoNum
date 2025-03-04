using AutoNumber.Infrastructure;
using AutoNumber.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;

namespace AutoNumber
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        DialogService dialogService = new();

        private void startup(object sender, StartupEventArgs e)
        {
            try
            {
                var mainVM = new MainVM(dialogService);
                var mainWin = new MainWindow(mainVM);
                mainWin.Show();
            }
            catch(Exception ex)             
            {
               MessageBox.Show(ex.Message);
            }

        }
    }

}
