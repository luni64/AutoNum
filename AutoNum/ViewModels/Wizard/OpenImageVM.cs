//using System.Windows;
using Emgu.CV;

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
                   var img = CvInvoke.Imread(imageFilename, Emgu.CV.CvEnum.ImreadModes.Color);
                    if (!img.IsEmpty)
                    {
                        parent.pictureVM = new ImageVM
                        {
                            emguImage = img,
                            ImageSource = img.ToBitmapSource(),
                            Filename = imageFilename, 
                        };
                    }                  
                    
                }
                catch { }                                             
            }
        }

        public override void Enter(object? o)
        {
            foreach (var mvs in parent.pictureVM.MarkerVMs)
            {
                mvs.visible = false;
            }
        }

        private MainVM parent = parent;
        private RelayCommand? _cmdChangeImage;              
    }
}
