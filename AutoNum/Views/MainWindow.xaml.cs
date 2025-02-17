using Microsoft.Win32;
using NumberIt.Infrastructure;
using NumberIt.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NumberIt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
      
        public MainWindow(MainVM mainVM)
        {
            InitializeComponent();

            this.DataContext = mainVM;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog()
            {

            };

            if(dialog.ShowDialog() == true && DataContext is MainVM mvm && mvm.CurrentStep is OpenImageVM vm)
            {
                vm.cmdChangeImage.Execute(dialog.FileName);

                
             

            }

        }
    }
}