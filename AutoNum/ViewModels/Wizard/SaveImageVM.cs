using AutoNumber.Model;
using System.IO;

namespace NumberIt.ViewModels
{
    public class SaveImageVM : WizardStep
    {
        public RelayCommand cmdSaveImage => _cmdSaveImage ??= new(doSaveImage);
        void doSaveImage(object? o)
        {
            if (parent.DialogService.ShowDialog(this) is string filename && !string.IsNullOrEmpty(filename))
            {
                if (filename != image.Filename) // we don't want to overwrite the original file
                {
                    using var mat = parent.pictureVM.toNumberedMat();
                    mat?.Save(filename);
                }
                else
                {
                    parent.DialogService.ShowDialog("Das Originalbild darf nicht überschrieben werden");
                }
            }
        }

        public string outputFile { get; set; }


        public SaveImageVM(MainVM parent)
        {
            this.parent = parent;

        }
        private RelayCommand? _cmdSaveImage;

        public override void Enter(object? o)
        {
            var filename = Path.GetFileNameWithoutExtension(image.Filename);
            var extension = Path.GetExtension(image.Filename);
            var folder = Path.GetDirectoryName(image.Filename) ?? ""; // null if rootdirectory

            outputFile = Path.Combine(folder, filename + "_NUM" + extension);
        }

        private MainVM parent;
        private ImageModel image => parent.pictureVM;
    }
}
