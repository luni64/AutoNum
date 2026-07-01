using MahApps.Metro.Controls;
using AutoNumber.ViewModels;
using AutoNumber.Views;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls.Dialogs;

namespace AutoNumber
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public static RoutedUICommand OpenFormatDialogCommand { get; } = new(nameof(OpenFormatDialogCommand), nameof(OpenFormatDialogCommand), typeof(MainWindow));
        public static RoutedUICommand OpenRowDefinitionDialogCommand { get; } = new(nameof(OpenRowDefinitionDialogCommand), nameof(OpenRowDefinitionDialogCommand), typeof(MainWindow));

        private RowDefinitionWindow? _rowDefinitionWindow;

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

            var settingsWindow = new SettingsWindow(mainVM.SettingsManager, mainVM)
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }

        private void OpenRowDefinitionDialog_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is RowDefinitionManager;
        }

        private void OpenRowDefinitionDialog_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is not RowDefinitionManager manager)
            {
                return;
            }

            if (_rowDefinitionWindow is null || !_rowDefinitionWindow.IsVisible)
            {
                _rowDefinitionWindow = new RowDefinitionWindow(manager)
                {
                    Owner = this
                };
                _rowDefinitionWindow.Closed += (_, _) => _rowDefinitionWindow = null;
                _rowDefinitionWindow.Show();
                return;
            }

            _rowDefinitionWindow.Activate();
        }

        private void ZoomToFit_Click(object sender, RoutedEventArgs e)
        {
            PictureDisplayControl.ZoomToFit();
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
                TitleManager manager => new TextFormatDialog(manager, "Überschrift formatieren", nameof(TitleManager.TitleFontColor), nameof(TitleManager.BackgroundColor), nameof(TitleManager.FontScale)),
                ImageInfoManager manager => new TextFormatDialog(manager, "Bildinformation formatieren", nameof(ImageInfoManager.ImageInfoFontColor), nameof(ImageInfoManager.BackgroundColor), nameof(ImageInfoManager.FontScale)),
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
