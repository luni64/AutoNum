//using NumberIt.Views;
//using NumberIt.ViewModels;
using System.Windows;
using System;
using NumberIt.ViewModels;
using Microsoft.Win32;
using System.IO;

namespace NumberIt.Infrastructure
{
    internal class DialogService : IDialogService
    {
        public object? ShowDialog(object viewModel)
        {
            object? retVal = null;
            switch (viewModel)
            {
                case OpenImageVM:
                    {
                        var dialog = new OpenFileDialog();
                        if (dialog.ShowDialog() == true)
                        {
                            
                            retVal = dialog.FileName;
                        }
                        break;
                    }

                case SaveImageVM:
                    {
                        var vm = (SaveImageVM) viewModel;
                        var dialog = new SaveFileDialog
                        {
                            FileName = Path.GetFileName(vm.outputFile),
                            InitialDirectory = Path.GetDirectoryName(vm.outputFile),
                            
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
