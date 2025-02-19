using System.Diagnostics;

namespace NumberIt.ViewModels
{
    public class OpenImageVM(MainVM parent) : WizardStep
    {
        public RelayCommand cmdChangeImage => _cmdChangeImage ??= new(doChangeImage);
        void doChangeImage(object? o)
        {
            if (parent.DialogService.ShowDialog(this) is string imageFilename)
            {
                try
                {
                    parent.pictureVM.Init(imageFilename);
                }
                catch { Trace.WriteLine("Error loading image in OpenImageVM"); }
            }
        }

        public override void Enter(object? o)
        {
            foreach (var marker in parent.pictureVM.MarkerVMs)
            {
                marker.visible = false;
            }
        }

        private MainVM parent = parent;
        private RelayCommand? _cmdChangeImage;
    }
}
