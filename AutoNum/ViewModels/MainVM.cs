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
        public LabelManager LabelManager { get; }
        public ImageModel PictureVM { get; }

        public IDialogService DialogService { get; }
        public string Title => $"AutoNumber V{Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion") ?? "x.x"}";

        public MainVM(IDialogService DialogService)
        {
            this.DialogService = DialogService;
            this.PictureVM = new(this);

            FileManager = new FileManager(this);            
            LabelManager = new LabelManager(this);
            NameManager = new NameManager(this);
            TitleManager = new TitleManager(this);
        }
    }   
}
