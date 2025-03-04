using MahApps.Metro.Controls;
using Microsoft.Win32;
using AutoNumber.ViewModels;
using System.Windows;

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
        }

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    var dialog = new OpenFileDialog()
        //    {

        //    };

        //    if(dialog.ShowDialog() == true && DataContext is MainVM mvm && mvm.CurrentStep is FileManager vm)
        //    {
        //        vm.cmdOpenImage.Execute(dialog.FileName);
        //    }

        //}

        private void Border_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(DataContext is MainVM mainVM)
            {
                mainVM.pictureVM.CanvasSize = new System.Drawing.Size((int)e.NewSize.Width, (int)e.NewSize.Height);
            }
        }
    }
}