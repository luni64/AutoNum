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
        public RelayCommand OpenImageCommand => _openImageCommand ??= new(ExecuteOpenImage);
        async void ExecuteOpenImage(object? o)
        {
            if (GetFilename(out string filename))
            {
                try
                {
                    var pvm = parent.PictureVM;
                    var bitmap = new Bitmap(filename);  // Dialog ensures that the file exists
                    var metadata = bitmap.getMetadata();

                    if (metadata == null)  // not written by AutoNumber => use as original image
                    {
                        var faces = FaceDetector.Detect(bitmap);
                        pvm.OriginalImageFilename = filename;
                        pvm.Bitmap = bitmap;
                        pvm.Init();
                        parent.LabelManager.SetLabels(faces);
                    }
                    else
                    {
                        if (!File.Exists(metadata.OriginalImage))  // we are AutoNumber generated but don't find the original file
                        {
                            string imagePath = await AskForOriginalFilename(metadata.OriginalImage);
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

        public RelayCommand SaveImageCommand => _saveImageCommand ??= new(ExecuteSaveImage);
        void ExecuteSaveImage(object? o)
        {
            var fullFilename = parent.PictureVM.OriginalImageFilename;
            var path = Path.GetDirectoryName(fullFilename)!;
            var file = Path.GetFileNameWithoutExtension(fullFilename);
            var extension = Path.GetExtension(fullFilename);

            var outputFile = file + "_num" + extension;
            var outputFolder = Path.Combine(path, outputFile);

            var saveFileInfo = new SaveFileInfo
            {
                Filename = outputFile,
                InitialDirectory = path,
                Filter = "JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg",
            };

            if (parent.DialogService.ShowDialog(saveFileInfo) is string filename && !string.IsNullOrEmpty(filename))
            {
                if (filename != parent.PictureVM.OriginalImageFilename)
                {
                    using var bmp = parent.PictureVM.toNumberedBitmap();
                    bmp?.Save(filename, ImageFormat.Jpeg);
                }
                else
                {
                    parent.DialogService.ShowDialog("Das Originalbild darf nicht überschrieben werden");
                }
            }
        }


        private async Task<string> AskForOriginalFilename(string orignalFilename)
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

            filename = parent.DialogService.ShowDialog(info) as string ?? string.Empty;            
            return  !string.IsNullOrEmpty(filename);
        }

        private MainVM parent { get; set; } = parent;
        private RelayCommand? _openImageCommand;
        private RelayCommand? _saveImageCommand;
    }
}
