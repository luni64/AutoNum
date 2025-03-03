using AutoNumber.Model;
using Emgu.CV;
using System.IO;

namespace NumberIt.ViewModels
{
    public class OpenFileInfo
    {
        public string? Filename { get; set; }
        public string? Filter { get; set; }
        public int FilterIndex { get; set; }

    }
    public class SaveFileInfo
    {
        public string? Filename { get; set; }
        public string? InitialDirectory { get; set; }
        public string? Filter { get; set; }
        public int FilterIndex { get; set; }
    }

    public class FileManager : BaseViewModel
    {
        public RelayCommand cmdOpenImage => _cmdOpenImage ??= new(doOpenImage);
        void doOpenImage(object? o)
        {
            var info = new OpenFileInfo
            {
                Filter = "All Image Files (*.bmp;*.png;*.tif;*.tiff;*.jpg;*.jpeg;*.gif)|*.bmp;*.png;*.tif;*.tiff;*.jpg;*.jpeg;*.gif|JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG Files (*.png)|*.png|TIFF Files (*.tif;*.tiff)|*.tif;*.tiff|GIF Files (*.gif)|*.gif|All Files (*.*)|*.*",
                FilterIndex = 1 // Sets "All Image Files" as the default filter
            };

            if (parent.DialogService.ShowDialog(info) is string imageFilename)
            {
                try
                {
                    parent.pictureVM.Init(imageFilename);   
                    var faces = FaceDetector.Detect(parent.pictureVM.Bitmap!);
                    parent.labelManager.SetLabels(faces);
                    
                }
                catch
                {
                    parent.DialogService.ShowDialog("Fehler beim Öffnen des Bildes");                    
                }
            }
        }

        public RelayCommand cmdSaveImage => _cmdSaveImage ??= new(doSaveImage);
        void doSaveImage(object? o)
        {
            var fullFilename = parent.pictureVM.Filename;
            var path = Path.GetDirectoryName(fullFilename)!;
            var file = Path.GetFileNameWithoutExtension(fullFilename);
            var extension = Path.GetExtension(fullFilename);

            var outputFile = file + "_num" + extension;
            var outputFolder = Path.Combine(path, outputFile);

            var saveFileInfo = new SaveFileInfo
            {
                Filename = outputFile,
                InitialDirectory = path,
                Filter = "All Image Files|*.bmp;*.png;*.tif;*.tiff;*.jpg;*.jpeg;*.gif|JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG Files (*.png)|*.png|TIFF Files (*.tif;*.tiff)|*.tif;*.tiff|GIF Files (*.gif)|*.gif|All Files (*.*)|*.*",
            };

            if (parent.DialogService.ShowDialog(saveFileInfo) is string filename && !string.IsNullOrEmpty(filename))
            {
                if (filename != parent.pictureVM.Filename) // we don't want to overwrite the original file
                {
                    using var mat = parent.pictureVM.toNumberedBitmap().ToMat();                    
                    mat?.Save(filename);
                }
                else
                {
                    parent.DialogService.ShowDialog("Das Originalbild darf nicht überschrieben werden");
                }
            }
        }             

        public FileManager(MainVM parent)
        {
            this.parent = parent;
        }

        private MainVM parent { get; set; }
        private RelayCommand? _cmdOpenImage;
        private RelayCommand? _cmdSaveImage;
    }
}
