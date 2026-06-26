using AutoNumber.Infrastructure;
using MahApps.Metro.Controls.Dialogs;

namespace AutoNumber.ViewModels
{
    public class MainVM : BaseViewModel
    {
        public IDialogCoordinator DialogCoordinator { get; set; } = null!;
        public FileManager FileManager { get; }
        public NameManager NameManager { get; }
        public TitleManager TitleManager { get; }
        public ImageInfoManager ImageInfoManager { get; }
        public ImageIdManager ImageIdManager { get; }
        public SettingsManager SettingsManager { get; }
        public LabelManager LabelManager { get; }
        public ImageVM PictureVM { get; }

        public IDialogService DialogService { get; }
        public string Title => $"AutoNumber V{Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion") ?? "x.x"}";

        public MainVM(IDialogService DialogService)
        {
            this.DialogService = DialogService;
            PictureVM = new ImageVM();

            LabelManager = new LabelManager(PictureVM);
            ImageIdManager = new ImageIdManager(LabelManager);
            NameManager = new NameManager(PictureVM, LabelManager, ImageIdManager);
            TitleManager = new TitleManager(LabelManager);
            ImageInfoManager = new ImageInfoManager(LabelManager);
            SettingsManager = new SettingsManager();
            FileManager = new FileManager(this);
        }
    }   
}
