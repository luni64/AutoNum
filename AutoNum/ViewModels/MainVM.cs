using AutoNumber.Infrastructure;
using MahApps.Metro.Controls.Dialogs;

namespace AutoNumber.ViewModels
{
    public class MainVM : BaseViewModel
    {
        public IDialogCoordinator DialogCoordinator 
        { 
            get => _dialogCoordinator;
            set
            {
                _dialogCoordinator = value;
                // Set dialog coordinator in LabelManager after it's available
                LabelManager._dialogCoordinator = value;
                LabelManager._mainVM = this;
            }
        }
        private IDialogCoordinator _dialogCoordinator = null!;

        public FileManager FileManager { get; }
        public NameManager NameManager { get; }
        public TitleManager TitleManager { get; }
        public ImageInfoManager ImageInfoManager { get; }
        public ImageIdManager ImageIdManager { get; }
        public SettingsManager SettingsManager { get; }
        public LabelManager LabelManager { get; }
        public RowDefinitionManager RowDefinitionManager { get; }
        public ImageVM PictureVM { get; }

        public IDialogService DialogService { get; }
        public string Title => string.IsNullOrEmpty(PictureVM.CurrentImageFilename)
            ? "AutoNumber V2.1.0"
            : $"AutoNumber V2.1.0  —  {System.IO.Path.GetFileName(PictureVM.CurrentImageFilename)}";

        public MainVM(IDialogService DialogService)
        {
            this.DialogService = DialogService;
            PictureVM = new ImageVM();
            PictureVM.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ImageVM.CurrentImageFilename))
                    OnPropertyChanged(nameof(Title));
            };

            LabelManager = new LabelManager(PictureVM);
            RowDefinitionManager = new RowDefinitionManager(PictureVM, LabelManager);
            ImageIdManager = new ImageIdManager(LabelManager);
            NameManager = new NameManager(PictureVM, LabelManager, ImageIdManager);
            TitleManager = new TitleManager(LabelManager);
            ImageInfoManager = new ImageInfoManager(LabelManager);
            SettingsManager = new SettingsManager();
            FileManager = new FileManager(this);
        }
    }
}
