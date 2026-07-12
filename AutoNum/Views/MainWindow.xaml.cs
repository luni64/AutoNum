using MahApps.Metro.Controls;
using AutoNumber.ViewModels;
using AutoNumber.Views;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace AutoNumber
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public static RoutedUICommand OpenFormatDialogCommand { get; } = new(nameof(OpenFormatDialogCommand), nameof(OpenFormatDialogCommand), typeof(MainWindow));

        public MainWindow(MainVM mainVM)
        {
            InitializeComponent();
            this.DataContext = mainVM;
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

            var settingsWindow = new SettingsWindow(mainVM.SettingsManager, mainVM)
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }

        private void ExportMetadata_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainVM mainVM)
            {
                return;
            }

            var exportedFile = mainVM.FileManager.ExportMetadataNow();
            if (!string.IsNullOrEmpty(exportedFile))
            {
                MessageBox.Show(this, $"Datei wurde exportiert:\n\n{exportedFile}", "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OpenManual_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://autonumber.niggl-schlagbauer.de/")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error opening manual link: {ex}");
            }
        }

        private void ZoomToFit_Click(object sender, RoutedEventArgs e)
        {
            PictureDisplayControl.ZoomToFit();
        }

        private void ZoomToImage_Click(object sender, RoutedEventArgs e)
        {
            PictureDisplayControl.ZoomToImage();
        }

        private void OpenFormatDialog_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is LabelManager or TitleManager or ImageInfoManager or ImageIdManager or NameManager;
        }

        private void OpenFormatDialog_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TextFormatDialog? dialog = e.Parameter switch
            {
                LabelManager manager => new TextFormatDialog(manager, "Etiketten formatieren", nameof(LabelManager.FontColor), nameof(LabelManager.BackgroundColor), nameof(LabelManager.LabelScale)),
                TitleManager manager => new TextFormatDialog(manager, "Überschrift formatieren", nameof(TitleManager.FontColor), nameof(TitleManager.BackgroundColor), nameof(TitleManager.FontScale)),
                ImageInfoManager manager => new TextFormatDialog(manager, "Bildinformation formatieren", nameof(ImageInfoManager.FontColor), nameof(ImageInfoManager.BackgroundColor), nameof(ImageInfoManager.FontScale)),
                ImageIdManager manager => new TextFormatDialog(manager, "Bild-ID formatieren", nameof(ImageIdManager.FontColor), nameof(ImageIdManager.BackgroundColor), nameof(ImageIdManager.FontScale)),
                NameManager manager => new TextFormatDialog(manager, "Namensliste formatieren", nameof(NameManager.FontColor), nameof(NameManager.BackgroundColor), nameof(NameManager.FontScale)),
                _ => null
            };

            if (dialog is null)
            {
                return;
            }

            dialog.Owner = this;
            dialog.ShowDialog();
        }
    }
}
