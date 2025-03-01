using AutoNumber.Model;
using Emgu.CV;
using System.Diagnostics;
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



    public class OpenImageVM : WizardStep
    {
        public RelayCommand cmdOpenImage => _cmdChangeImage ??= new(doChangeImage);
        void doChangeImage(object? o)
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

                    var img = parent.pictureVM.Bitmap;

                    var hres = img.HorizontalResolution;
                    var widthpx = img.Width;
                    var widthInch = widthpx / hres;

                    var fh = hres * 12.0 / 72;


                    Trace.WriteLine($"{hres} {widthpx} {widthInch} {fh}");
                    // add flag isOpen here
                    parent.analyzeVM.doAnalyze();
                    parent.labelsVM.Enter(null);
                }
                catch
                {
                    parent.DialogService.ShowDialog("Fehler beim Öffnen des Bildes");
                    Trace.WriteLine("Error loading image in OpenImageVM");
                }
            }

        }


        RelayCommand? _cmdSaveImage;
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
                    using var mm = parent.pictureVM.toNumberedBitmap();
                    using var mat = mm.ToMat();
                    //using var mat = parent.pictureVM.toNumberedMat();
                    mat?.Save(filename);
                }
                else
                {
                    parent.DialogService.ShowDialog("Das Originalbild darf nicht überschrieben werden");
                }
            }
        }


        public override void Enter(object? o)
        {
            foreach (var marker in parent.pictureVM.MarkerVMs)
            {
                marker.visible = false;
            }
        }

        public MainVM parent { get; set; }
        private RelayCommand? _cmdChangeImage;

        public OpenImageVM(MainVM parent)
        {
            this.parent = parent;
        }
    }
}
