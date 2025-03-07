using AutoNumber.Infrastructure;
using MahApps.Metro.Controls.Dialogs;

namespace AutoNumber.ViewModels
{
    public class MainVM : BaseViewModel
    {
        public IDialogCoordinator DialogCoordinator { get; set; } = null!;
        public FileManager fileManager { get; }
        public NameManager nameManager { get; }
        public TitleManager titleManager { get; }       
        public LabelManager labelManager { get; }
        public ImageModel pictureVM { get; }
        
        public IDialogService DialogService { get; }
        public string Title => $"AutoNumber V{Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion") ?? "x.x"}";

        public MainVM(IDialogService DialogService)
        {
            this.DialogService = DialogService;
            this.pictureVM = new(this);

            fileManager = new FileManager(this);            
            labelManager = new LabelManager(this);
            nameManager = new NameManager(this);
            titleManager = new TitleManager(this);
        }
    }   
}
