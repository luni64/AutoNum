using Microsoft.Win32;
using AutoNumber.ViewModels;
using System.IO;
using System.Windows;

namespace AutoNumber.Infrastructure
{
    internal class DialogService : IDialogService
    {
        public object? ShowDialog(object viewModel)
        {
            object? retVal = null;
            switch (viewModel)
            {
                case OpenFileInfo:
                    {
                        var vm = (OpenFileInfo)viewModel;
                        var dialog = new OpenFileDialog
                        {
                            Filter = vm.Filter,
                            CheckFileExists = true,       
                            ForcePreviewPane = true,
                        };
                        if (dialog.ShowDialog() == true)
                        {
                            retVal = dialog.FileName;                            
                        }
                        break;
                    }

                case SaveFileInfo:
                    {
                        var vm = (SaveFileInfo)viewModel;
                        var dialog = new SaveFileDialog
                        {
                            FileName = vm.Filename,
                            InitialDirectory = vm.InitialDirectory,
                            Filter = vm.Filter,
                        };
                        retVal = dialog.ShowDialog() == true ? dialog.FileName : null;
                        break;
                    }

                case string errorMsg:
                    MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
            return retVal;
        }
    }
}
