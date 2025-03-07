using AutoNumber.Model;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace AutoNumber.ViewModels
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

    public class FileManager(MainVM parent) : BaseViewModel
    {
        public RelayCommand cmdOpenImage => _cmdOpenImage ??= new(doOpenImage);
        async void doOpenImage(object? o)
        {
            if (GetFilename(out string filename))
            {
                try
                {
                    var pvm = parent.pictureVM;
                    var bitmap = new Bitmap(filename);  // Dialog ensures that the file exists
                    var metadata = bitmap.getMetadata();

                    if (metadata == null)  // not written by AutoNumber => use as original image
                    {
                        var faces = FaceDetector.Detect(bitmap);
                        pvm.OriginalImageFilename = filename;
                        pvm.Bitmap = bitmap;
                        pvm.Init();
                        parent.labelManager.SetLabels(faces);
                    }
                    else
                    {
                        if (!File.Exists(metadata.OriginalImage))  // we are AutoNumber generated but don't find the original file
                        {
                            string imagePath = await askForOriginalFilename(metadata.OriginalImage); // ask user to search for the image
                            if (string.IsNullOrEmpty(imagePath)) throw new FileNotFoundException();
                            metadata.OriginalImage = imagePath;
                        }

                        bitmap.Dispose(); // we will load the original image instead of the numbered copy
                        pvm.Bitmap = new Bitmap(metadata.OriginalImage);
                        pvm.OriginalImageFilename = metadata.OriginalImage;
                        pvm.InitFromMetadata(metadata);
                    }
                }
                catch (InvalidOperationException) { Trace.WriteLine("No faces found"); }
                catch
                {
                    await parent.DialogCoordinator!.ShowMessageAsync(parent, "Fehler", "Fehler beim Öffnen des Bildes");
                }
            }
        }

        public RelayCommand cmdSaveImage => _cmdSaveImage ??= new(doSaveImage);
        void doSaveImage(object? o)
        {
            var fullFilename = parent.pictureVM.OriginalImageFilename;
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
                if (filename != parent.pictureVM.OriginalImageFilename) // we don't want to overwrite the original file
                {
                    using var bmp = parent.pictureVM.toNumberedBitmap();
                    bmp.Save(filename, ImageFormat.Jpeg);
                }
                else
                {
                    parent.DialogService.ShowDialog("Das Originalbild darf nicht überschrieben werden");
                }
            }
        }


        private async Task<string> askForOriginalFilename(string orignalFilename)
        {
            var settings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Ja",
                NegativeButtonText = "Nein",
                ColorScheme = MetroDialogColorScheme.Theme,
                DefaultButtonFocus = MessageDialogResult.Affirmative,
                Icon = new PackIconMaterial
                {
                    Kind = PackIconMaterialKind.FileQuestionOutline,
                    Width = 64,
                    Height = 64
                }
            };

            var result = await parent.DialogCoordinator.ShowMessageAsync(parent,
                "Original Bild nicht gefunden!",
                $"Sie haben versucht, ein von AutoNumber erstelltes Bild zu öffnen. Um dieses weiter bearbeiten zu können, " +
                $"wird das Originalbild benötigt. AutoNumber konnte dieses Bild nicht finden. " +
                $"Möchten Sie das Originalbild selbst suchen?", MessageDialogStyle.AffirmativeAndNegative, settings);

            if (result == MessageDialogResult.Affirmative)
            {
                var info = new OpenFileInfo
                {
                    Filter = "All Image Files (*.bmp;*.png;*.tif;*.tiff;*.jpg;*.jpeg;*.gif)|*.bmp;*.png;*.tif;*.tiff;*.jpg;*.jpeg;*.gif|JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG Files (*.png)|*.png|TIFF Files (*.tif;*.tiff)|*.tif;*.tiff|GIF Files (*.gif)|*.gif|All Files (*.*)|*.*",
                    FilterIndex = 1, // Sets "All Image Files" as the default filter                
                };
                return GetFilename(out string filename) ? filename : string.Empty;
            }
            return string.Empty;
        }
        private bool GetFilename(out string filename)
        {
            var info = new OpenFileInfo
            {
                Filter = "All Image Files (*.bmp;*.png;*.tif;*.tiff;*.jpg;*.jpeg;*.gif)|*.bmp;*.png;*.tif;*.tiff;*.jpg;*.jpeg;*.gif|JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG Files (*.png)|*.png|TIFF Files (*.tif;*.tiff)|*.tif;*.tiff|GIF Files (*.gif)|*.gif|All Files (*.*)|*.*",
                FilterIndex = 1, // Sets "All Image Files" as the default filter                
            };

            var result = parent.DialogService.ShowDialog(info) as string;
            filename = result != null ? result : string.Empty;
            return filename != null;
        }

        private MainVM parent { get; set; } = parent;
        private RelayCommand? _cmdOpenImage;
        private RelayCommand? _cmdSaveImage;
    }
}
