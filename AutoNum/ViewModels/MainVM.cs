using AutoNumber.Infrastructure;
using MahApps.Metro.Controls.Dialogs;

namespace AutoNumber.ViewModels
{
    public class MainVM : BaseViewModel
    {
        public IDialogCoordinator DialogCoordinator { get; }

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

        /// <summary>App name + version from the csproj's Version property — the single source.</summary>
        private static readonly string AppTitle =
            $"AutoNumber V{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?"}";

        public string Title => string.IsNullOrEmpty(PictureVM.CurrentImageFilename)
            ? AppTitle
            : $"{AppTitle}  —  {System.IO.Path.GetFileName(PictureVM.CurrentImageFilename)}";

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

            // MahApps' coordinator is a stateless singleton; the window only has to register
            // itself via DialogParticipation.Register in XAML. Wiring it (and the back-reference)
            // here removes the old temporal coupling where MainWindow's constructor had to
            // remember to assign DialogCoordinator right after creating the VM.
            DialogCoordinator = MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
            LabelManager._dialogCoordinator = DialogCoordinator;
            LabelManager._mainVM = this;
        }
    }
}
