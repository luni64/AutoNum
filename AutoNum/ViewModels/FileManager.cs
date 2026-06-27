using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
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
            try
            {
                if (!GetFilename(out string filename)) return;

                var pvm = parent.PictureVM;
                var bitmap = BitmapExtensions.LoadBitmapFromFile(filename);
                bitmap.ApplyExifOrientation();
                var metadata = bitmap.GetMetadata();

                if (metadata is null)  // not written by AutoNumber => use as original image
                {
                    var faces = FaceDetector.Detect(bitmap);
                    pvm.OriginalImageFilename = filename;
                    pvm.CurrentImageFilename = filename;
                    pvm.OriginalPropertyItems = bitmap.PropertyItems;
                    pvm.Bitmap = bitmap;
                    pvm.Init();
                    WeakReferenceMessenger.Default.Send(new NewImageOpenedMessage(faces));
                    parent.SettingsManager.ApplyFreshImageDefaults(parent.LabelManager, parent.NameManager, parent.TitleManager, parent.ImageInfoManager, parent.ImageIdManager);
                }
                else if (metadata is AutoNumMetaData_V2 v2)
                {
                    // V2: self-contained — restore clean base image from embedded patches
                    var fileBytes = File.ReadAllBytes(filename);
                    var patches = AppSegmentIO.ReadSegments(fileBytes);

                    if (patches is not null && patches.Count > 0)
                    {
                        var restored = bitmap.RestoreFromPatches(v2, patches);
                        bitmap.Dispose();

                        pvm.OriginalPropertyItems = restored.PropertyItems;
                        pvm.Bitmap = restored;
                        pvm.OriginalImageFilename = string.IsNullOrWhiteSpace(v2.OriginalImage) ? filename : v2.OriginalImage;
                        pvm.CurrentImageFilename = filename;
                        pvm.InitFromMetadata(v2);
                    }
                    else
                    {
                        // V2 without patches — fall back to V1 original-file flow
                        await openFromOriginalFile(bitmap, metadata, pvm, filename);
                    }
                }
                else
                {
                    // V1: needs the original file on disk
                    await openFromOriginalFile(bitmap, metadata, pvm, filename);
                }
            }
            catch (InvalidOperationException) { Trace.WriteLine("No faces found"); }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error opening image: {ex}");
                try
                {
                    await parent.DialogCoordinator!.ShowMessageAsync(parent, "Fehler", "Fehler beim Öffnen des Bildes");
                }
                catch (Exception dlgEx)
                {
                    Trace.WriteLine($"Error showing dialog: {dlgEx}");
                }
            }
        }

        public RelayCommand SaveImageCommand => _saveImageCommand ??= new(ExecuteSaveImage);
        void ExecuteSaveImage(object? o)
        {
            var fullFilename = !string.IsNullOrWhiteSpace(parent.PictureVM.CurrentImageFilename)
                ? parent.PictureVM.CurrentImageFilename
                : parent.PictureVM.OriginalImageFilename;
            var path = Path.GetDirectoryName(fullFilename)!;
            var file = Path.GetFileNameWithoutExtension(fullFilename);
            var extension = Path.GetExtension(fullFilename);
            var isEditingProtectedOriginal = IsProtectedOriginalPath(fullFilename, parent.PictureVM.OriginalImageFilename);

            var outputFile = (isEditingProtectedOriginal && parent.SettingsManager.AppendNumSuffixForOriginalSaves)
                ? file + "_num" + extension
                : Path.GetFileName(fullFilename);

            var saveFileInfo = new SaveFileInfo
            {
                Filename = outputFile,
                InitialDirectory = path,
                Filter = "JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg",
            };

            if (parent.DialogService.ShowDialog(saveFileInfo) is string filename && !string.IsNullOrEmpty(filename))
            {
                if (IsProtectedOriginalPath(filename, parent.PictureVM.OriginalImageFilename))
                {
                    parent.DialogService.ShowDialog("Das Originalbild darf nicht überschrieben werden");
                    return;
                }

                using var result = parent.PictureVM.ToNumberedBitmap(parent.LabelManager, parent.NameManager, parent.TitleManager, parent.ImageInfoManager, parent.ImageIdManager);
                if (result is null) return;

                // Encode bitmap to JPEG in memory, then inject APP4 patch segments
                using var jpegStream = new MemoryStream();
                result.Bitmap.Save(jpegStream, ImageFormat.Jpeg);
                var jpegBytes = jpegStream.ToArray();

                var finalBytes = AppSegmentIO.InjectSegments(jpegBytes, result.Patches);
                File.WriteAllBytes(filename, finalBytes);
                parent.PictureVM.CurrentImageFilename = filename;
            }
        }


        private async Task openFromOriginalFile(Bitmap numberedBitmap, AutoNumMetaData_V1 metadata, ImageVM pvm, string currentFilename)
        {
            if (!File.Exists(metadata.OriginalImage))
            {
                string imagePath = await AskForOriginalFilename(metadata.OriginalImage);
                if (string.IsNullOrEmpty(imagePath)) throw new FileNotFoundException();
                metadata.OriginalImage = imagePath;
            }

            numberedBitmap.Dispose();
            var originalBitmap = BitmapExtensions.LoadBitmapFromFile(metadata.OriginalImage);
            originalBitmap.ApplyExifOrientation();
            pvm.OriginalPropertyItems = originalBitmap.PropertyItems;
            pvm.Bitmap = originalBitmap;
            pvm.OriginalImageFilename = metadata.OriginalImage;
            pvm.CurrentImageFilename = currentFilename;
            pvm.InitFromMetadata(metadata);
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

        private static bool IsProtectedOriginalPath(string selectedPath, string protectedOriginalPath)
        {
            if (string.IsNullOrWhiteSpace(selectedPath) || string.IsNullOrWhiteSpace(protectedOriginalPath))
            {
                return false;
            }

            var selectedFullPath = Path.GetFullPath(selectedPath);
            var protectedFullPath = Path.GetFullPath(protectedOriginalPath);
            return string.Equals(selectedFullPath, protectedFullPath, StringComparison.OrdinalIgnoreCase);
        }

        private MainVM parent { get; set; } = parent;
        private RelayCommand? _openImageCommand;
        private RelayCommand? _saveImageCommand;
    }
}
