using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using System.Diagnostics;
using System.Drawing;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Collections.Generic;

namespace AutoNumber.ViewModels
{
    public class OpenFileInfo
    {
        public string? Filename { get; set; }
        public string? InitialDirectory { get; set; }
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
        public bool ExportCsvMetadata => parent.SettingsManager.ExportCsvMetadata;

        public bool ExportJsonMetadata => parent.SettingsManager.ExportJsonMetadata;

        public RelayCommand OpenImageCommand => _openImageCommand ??= new(ExecuteOpenImage);
        async void ExecuteOpenImage(object? o)
        {
            try
            {
                if (!GetFilename(out string filename))
                {
                    return;
                }

                Trace.WriteLine($"OpenImage: start '{filename}'");
                var pvm = parent.PictureVM;

                if (string.Equals(Path.GetExtension(filename), ".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    Trace.WriteLine("OpenImage: detected PDF input");
                    OpenFromPdfFile(filename, pvm);
                    Trace.WriteLine("OpenImage: PDF import completed");
                    return;
                }

                var bitmap = BitmapExtensions.LoadBitmapFromFile(filename);
                bitmap.ApplyExifOrientation();
                var metadata = bitmap.GetMetadata();
                Trace.WriteLine($"OpenImage: metadata version = '{metadata?.Version ?? "none"}'");

                if (metadata is null)  // not written by AutoNumber => use as original image
                {
                    pvm.OriginalImageFilename = filename;
                    pvm.CurrentImageFilename = filename;
                    pvm.OriginalPropertyItems = bitmap.PropertyItems;
                    pvm.Bitmap = bitmap;
                    pvm.Init();

                    if (parent.SettingsManager.FaceDetectionEnabled)
                    {
                        Trace.WriteLine("OpenImage: no AutoNum metadata, running face detection");
                        var faces = FaceDetector.Detect(bitmap);
                        WeakReferenceMessenger.Default.Send(new NewImageOpenedMessage(faces));
                        Trace.WriteLine($"OpenImage: fresh image initialized with {faces.Count} detected face(s)");
                    }
                    else
                    {
                        Trace.WriteLine("OpenImage: no AutoNum metadata, face detection disabled");
                        // No faces to report, but LabelManager still needs this to (re)compute
                        // BaseLabelDiameter for the new image via the fallback formula — otherwise
                        // it would keep whatever diameter was left over from a previous photo.
                        WeakReferenceMessenger.Default.Send(new NewImageOpenedMessage([]));
                    }

                    parent.SettingsManager.ApplyFreshImageDefaults(parent.LabelManager, parent.NameManager, parent.TitleManager, parent.ImageInfoManager, parent.ImageIdManager);
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
                else if (metadata is AutoNumMetaData_V2 v2)
                {
                    // V2: self-contained — restore clean base image from embedded patches
                    var fileBytes = File.ReadAllBytes(filename);
                    var patches = AppSegmentIO.ReadSegments(fileBytes);

                    if (patches is not null && patches.Count > 0)
                    {
                        Trace.WriteLine($"OpenImage: metadata restore from JPEG APP4 patches ({patches.Count} patch(es))");
                        var restored = bitmap.RestoreFromPatches(v2, patches);
                        bitmap.Dispose();

                        pvm.OriginalPropertyItems = restored.PropertyItems;
                        pvm.Bitmap = restored;
                        pvm.OriginalImageFilename = string.IsNullOrWhiteSpace(v2.OriginalImage) ? filename : v2.OriginalImage;
                        pvm.CurrentImageFilename = filename;
                        pvm.InitFromMetadata(v2);

                        RefreshPreviewAfterMetadataLoad("OpenImage/JPEG");
                        Trace.WriteLine("OpenImage: metadata restore from JPEG completed");
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
            catch (Exception ex)
            {
                Trace.WriteLine($"Error opening image: {ex}");
                try
                {
                    await parent.DialogCoordinator!.ShowMessageAsync(parent, "Fehler", $"Fehler beim Öffnen des Bildes: {ex.Message}");
                }
                catch (Exception dlgEx)
                {
                    Trace.WriteLine($"Error showing dialog: {dlgEx}");
                }
            }
        }

        private const string SaveAsFilter = "JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PDF Files (*.pdf)|*.pdf";
        private const string ExportMetadataFilter = "CSV Files (*.csv)|*.csv|JSON Files (*.json)|*.json";

        public RelayCommand SaveCommand => _saveCommand ??= new(ExecuteSave, CanExecuteSave);
        public RelayCommand SaveAsCommand => _saveAsCommand ??= new(ExecuteSaveAs);

        private bool CanExecuteSave(object? o) =>
            !string.IsNullOrWhiteSpace(parent.PictureVM.CurrentImageFilename)
            && !IsProtectedOriginalPath(parent.PictureVM.CurrentImageFilename, parent.PictureVM.OriginalImageFilename);

        private void ExecuteSave(object? o) => WriteJpgOrPdf(parent.PictureVM.CurrentImageFilename);

        private void ExecuteSaveAs(object? o)
        {
            var fullFilename = GetCurrentSaveFilename();
            var defaultFormat = parent.SettingsManager.DefaultSaveFormat;
            var suggestedExtension = defaultFormat == SaveFormat.Pdf ? ".pdf" : ".jpg";
            var filterIndex = defaultFormat == SaveFormat.Pdf ? 2 : 1;
            var saveFileInfo = CreateSaveFileInfo(fullFilename, suggestedExtension, SaveAsFilter, filterIndex);

            if (parent.DialogService.ShowDialog(saveFileInfo) is string filename && !string.IsNullOrEmpty(filename))
            {
                if (IsProtectedOriginalPath(filename, parent.PictureVM.OriginalImageFilename))
                {
                    parent.DialogService.ShowDialog("Das Originalbild darf nicht überschrieben werden");
                    return;
                }

                WriteJpgOrPdf(filename);
            }
        }

        private void WriteJpgOrPdf(string filename)
        {
            var extension = Path.GetExtension(filename);
            if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                WritePdfWithSidecars(filename);
            }
            else if (string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                WriteJpg(filename);
            }
            else
            {
                parent.DialogService.ShowDialog($"Nicht unterstütztes Dateiformat '{extension}'. Bitte '.jpg' oder '.pdf' verwenden.");
                return;
            }

            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private void WriteJpg(string filename)
        {
            using var result = parent.PictureVM.ToNumberedBitmap(parent.LabelManager, parent.NameManager, parent.TitleManager, parent.ImageInfoManager, parent.ImageIdManager);
            if (result is null)
            {
                parent.DialogService.ShowDialog("Speichern nicht möglich — es ist kein Bild geladen.");
                return;
            }

            // Encode bitmap to JPEG in memory, then inject APP4 patch segments
            using var jpegStream = new MemoryStream();
            result.Bitmap.Save(jpegStream, DrawingImageFormat.Jpeg);
            var jpegBytes = jpegStream.ToArray();

            var finalBytes = AppSegmentIO.InjectSegments(jpegBytes, result.Patches);

            if (!TryWriteWithRetry(filename, () => File.WriteAllBytes(filename, finalBytes), "JPEG"))
            {
                return;
            }

            parent.PictureVM.CurrentImageFilename = filename;

            var exportData = BuildExportData();
            exportData.GeneratedAt = DateTimeOffset.Now.ToString("O");
            WriteMetadataSidecars(filename, exportData, result);
        }

        private void WritePdfWithSidecars(string filename)
        {
            using var result = parent.PictureVM.ToNumberedBitmap(parent.LabelManager, parent.NameManager, parent.TitleManager, parent.ImageInfoManager, parent.ImageIdManager);
            if (result is null)
            {
                parent.DialogService.ShowDialog("Speichern nicht möglich — es ist kein Bild geladen.");
                return;
            }

            var exportData = BuildExportData();
            exportData.GeneratedAt = DateTimeOffset.Now.ToString("O");
            if (!WritePdf(filename, exportData))
            {
                return;
            }

            parent.PictureVM.CurrentImageFilename = filename;
            WriteMetadataSidecars(filename, exportData, result);
        }

        /// <summary>
        /// Opens a Save-As-style dialog for a CSV or JSON metadata sidecar, decoupled from the
        /// ExportCsvMetadata/ExportJsonMetadata auto-export-on-save toggles. Returns the written
        /// path, or null if the user cancelled or the write failed.
        /// </summary>
        public string? ExportMetadataNow()
        {
            var fullFilename = GetCurrentSaveFilename();
            if (string.IsNullOrWhiteSpace(fullFilename))
            {
                return null;
            }

            var saveFileInfo = CreateSaveFileInfo(fullFilename, ".csv", ExportMetadataFilter, 1);

            if (parent.DialogService.ShowDialog(saveFileInfo) is not string filename || string.IsNullOrEmpty(filename))
            {
                return null;
            }

            var exportData = BuildExportData();
            exportData.GeneratedAt = DateTimeOffset.Now.ToString("O");

            var extension = Path.GetExtension(filename);
            var ok = string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase)
                ? TryWriteWithRetry(filename, () => WriteJson(filename, exportData), "JSON")
                : TryWriteWithRetry(filename, () => WriteCsv(filename, exportData), "CSV");

            return ok ? filename : null;
        }

        private SidecarExportData BuildExportData()
        {
            var persons = parent.PictureVM.Persons
                .OrderBy(p => p.Label.Number)
                .Select(p => new SidecarPerson
                {
                    Row = p.Row,
                    Number = p.Label.Number,
                    Name = string.IsNullOrWhiteSpace(p.Name.Text) ? string.Empty : p.Name.Text
                })
                .ToList();

            return new SidecarExportData
            {
                Title = parent.TitleManager.Title ?? string.Empty,
                Description = parent.ImageInfoManager.ImageInfo ?? string.Empty,
                Id = parent.ImageIdManager.ImageId ?? string.Empty,
                Persons = persons
            };
        }

        private void WriteMetadataSidecars(string imageFilename, SidecarExportData exportData, NumberedBitmapResult numberedBitmapResult)
        {
            if (ExportCsvMetadata)
            {
                var csvPath = Path.ChangeExtension(imageFilename, ".csv");
                if (!TryWriteWithRetry(csvPath, () => WriteCsv(csvPath, exportData), "CSV"))
                {
                    return;
                }
            }

            if (ExportJsonMetadata)
            {
                var jsonPath = Path.ChangeExtension(imageFilename, ".json");
                if (!TryWriteWithRetry(jsonPath, () => WriteJson(jsonPath, exportData), "JSON"))
                {
                    return;
                }
            }
        }

        private static void WriteCsv(string filename, SidecarExportData exportData)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"GeneratedAt;{EscapeCsv(exportData.GeneratedAt)}");
            builder.AppendLine($"Title;{EscapeCsv(exportData.Title)}");
            builder.AppendLine($"Description;{EscapeCsv(exportData.Description)}");
            builder.AppendLine($"ID;{EscapeCsv(exportData.Id)}");
            builder.AppendLine();
            builder.AppendLine("Row;Number;Name");

            foreach (var person in exportData.Persons)
            {
                builder.AppendLine($"{person.Row};{person.Number};{EscapeCsv(person.Name)}");
            }

            File.WriteAllText(filename, builder.ToString(), new UTF8Encoding(true));
        }

        private static void WriteJson(string filename, SidecarExportData exportData)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(exportData, options);
            File.WriteAllText(filename, json, new UTF8Encoding(true));
        }

        private bool TryWriteWithRetry(string filename, Action writeAction, string operationName)
        {
            while (true)
            {
                try
                {
                    writeAction();
                    return true;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    if (!parent.DialogService.ShowSaveRetryDialog(operationName, filename, ex.Message))
                    {
                        return false;
                    }
                }
            }
        }

        private static string EscapeCsv(string value)
        {
            var text = value ?? string.Empty;
            var mustQuote = text.Contains(';') || text.Contains('"') || text.Contains('\n') || text.Contains('\r');
            if (!mustQuote)
            {
                return text;
            }

            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        private bool WritePdf(string filename, SidecarExportData exportData)
        {
            byte[]? photoBytes = null;
            using (var photoWithLabels = parent.PictureVM.ToPhotoWithLabelsBitmap())
            {
                if (photoWithLabels is not null)
                {
                    using var imageStream = new MemoryStream();
                    photoWithLabels.Save(imageStream, DrawingImageFormat.Png);
                    photoBytes = imageStream.ToArray();
                }
            }

            var pdfBytes = PdfReportRenderer.Render(
                exportData,
                photoBytes,
                parent.NameManager.NameTableColumnCount,
                parent.NameManager.ShowRowDividers,
                parent.NameManager.FormatRowDividerText);

            // Update existing metadata with latest runtime values, then use it
            parent.PictureVM.UpdateMetadataBeforeSave(parent.LabelManager, parent.NameManager, parent.TitleManager, parent.ImageInfoManager, parent.ImageIdManager);
            var metadata = parent.PictureVM.CurrentMetadata!;

            if (parent.PictureVM.Bitmap is null)
            {
                throw new InvalidOperationException("Die Basisgrafik für den PDF-Export ist nicht verfügbar.");
            }

            using var baseImageStream = new MemoryStream();
            parent.PictureVM.Bitmap.Save(baseImageStream, DrawingImageFormat.Jpeg);

            var payloadZip = PdfPayloadStore.CreatePayloadZip(new PdfPayloadData
            {
                Metadata = metadata,
                BaseImageBytes = baseImageStream.ToArray()
            });

            if (!PdfPayloadStore.TryReadPayloadZip(payloadZip, out var payloadCheck)
                || payloadCheck is null
                || payloadCheck.Metadata is null
                || payloadCheck.BaseImageBytes.Length == 0)
            {
                throw new InvalidDataException("Die PDF-Nutzdaten konnten nicht verifiziert werden.");
            }

            return TryWriteWithRetry(filename, () => PdfPayloadStore.SavePdfWithPayloadAttachment(pdfBytes, payloadZip, filename), "PDF");
        }

        private void OpenFromPdfFile(string pdfFilename, ImageVM pvm)
        {
            Trace.WriteLine($"OpenFromPdfFile: reading '{pdfFilename}'");
            if (!PdfPayloadStore.TryExtractPayloadFromPdfAttachment(pdfFilename, out var payloadZipBytes) || payloadZipBytes is null)
            {
                throw new InvalidDataException("Die PDF enthält keine editierbaren AutoNum-Daten.");
            }

            Trace.WriteLine($"OpenFromPdfFile: extracted payload zip ({payloadZipBytes.Length} bytes)");
            if (!PdfPayloadStore.TryReadPayloadZip(payloadZipBytes, out var payload) || payload is null)
            {
                throw new InvalidDataException("Die eingebetteten AutoNum-Daten in der PDF sind ungültig.");
            }

            Trace.WriteLine($"OpenFromPdfFile: payload metadata version '{payload.Metadata.Version}', base image size={payload.BaseImageBytes.Length} bytes");

            using var baseStream = new MemoryStream(payload.BaseImageBytes);
            using var baseSource = new Bitmap(baseStream);
            var restoredBitmap = new Bitmap(baseSource);

            pvm.OriginalPropertyItems = restoredBitmap.PropertyItems;
            pvm.Bitmap = restoredBitmap;
            pvm.OriginalImageFilename = string.IsNullOrWhiteSpace(payload.Metadata.OriginalImage)
                ? pdfFilename
                : payload.Metadata.OriginalImage;
            pvm.CurrentImageFilename = pdfFilename;
            pvm.InitFromMetadata(payload.Metadata);
            RefreshPreviewAfterMetadataLoad("OpenFromPdfFile");
            Trace.WriteLine("OpenFromPdfFile: metadata initialization completed");
        }

        private async Task openFromOriginalFile(Bitmap numberedBitmap, AutoNumMetaData_V1 metadata, ImageVM pvm, string currentFilename)
        {
            Trace.WriteLine($"openFromOriginalFile: requested original '{metadata.OriginalImage}'");

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
            RefreshPreviewAfterMetadataLoad("openFromOriginalFile");
            Trace.WriteLine("openFromOriginalFile: metadata initialization completed");
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
                Filter = "AutoNum Dateien (*.bmp;*.png;*.tif;*.tiff;*.jpg;*.jpeg;*.gif;*.pdf)|*.bmp;*.png;*.tif;*.tiff;*.jpg;*.jpeg;*.gif;*.pdf|JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG Files (*.png)|*.png|TIFF Files (*.tif;*.tiff)|*.tif;*.tiff|GIF Files (*.gif)|*.gif|PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
                FilterIndex = 1, // Sets AutoNum-compatible files as the default filter
                InitialDirectory = GetLastOpenFolder(),
            };

            filename = parent.DialogService.ShowDialog(info) as string ?? string.Empty;
            return  !string.IsNullOrEmpty(filename);
        }

        /// <summary>
        /// Folder of the currently loaded photo's original file, so the next Open dialog
        /// defaults back there — not wherever a subsequent Speichern-unter/custom output
        /// folder last redirected to, which would otherwise force browsing back manually
        /// when numbering a whole batch of photos from one source folder.
        /// </summary>
        private string? GetLastOpenFolder()
        {
            var originalFilename = parent.PictureVM.OriginalImageFilename;
            if (string.IsNullOrWhiteSpace(originalFilename))
            {
                return null;
            }

            var folder = Path.GetDirectoryName(originalFilename);
            return !string.IsNullOrEmpty(folder) && Directory.Exists(folder) ? folder : null;
        }

        private void RefreshPreviewAfterMetadataLoad(string source)
        {
            Trace.WriteLine($"RefreshPreviewAfterMetadataLoad: source={source}");
            parent.NameManager.RefreshAndShowNames();
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private SaveFileInfo CreateSaveFileInfo(string fullFilename, string extension, string filter, int filterIndex)
        {
            var sourcePath = Path.GetDirectoryName(fullFilename)!;
            var file = Path.GetFileNameWithoutExtension(fullFilename);
            var isEditingProtectedOriginal = IsProtectedOriginalPath(fullFilename, parent.PictureVM.OriginalImageFilename);

            var outputFile = isEditingProtectedOriginal && !string.IsNullOrWhiteSpace(parent.SettingsManager.SaveFileSuffix)
                ? file + parent.SettingsManager.SaveFileSuffix + extension
                : file + extension;

            return new SaveFileInfo
            {
                Filename = outputFile,
                InitialDirectory = ResolveOutputFolder(sourcePath),
                Filter = filter,
                FilterIndex = filterIndex,
            };
        }

        private string GetCurrentSaveFilename()
        {
            return !string.IsNullOrWhiteSpace(parent.PictureVM.CurrentImageFilename)
                ? parent.PictureVM.CurrentImageFilename
                : parent.PictureVM.OriginalImageFilename;
        }

        private string ResolveOutputFolder(string sourcePath)
        {
            var configuredFolder = parent.SettingsManager.OutputFolder;
            if (!parent.SettingsManager.UseCustomOutputFolder || string.IsNullOrWhiteSpace(configuredFolder))
            {
                return sourcePath;
            }

            if (Path.IsPathFullyQualified(configuredFolder))
            {
                // Absolute paths are set via the Browse button and must already exist.
                return Directory.Exists(configuredFolder) ? Path.GetFullPath(configuredFolder) : sourcePath;
            }

            // Relative paths (e.g. "AutoNum" or "AutoNum/test") are a subfolder next to the
            // source image, created on demand. Path.IsPathFullyQualified (not IsPathRooted) is
            // required here: a driveless path like "/AutoNum" is "rooted" but not fully
            // qualified, so it also falls into this relative branch rather than being misread
            // as absolute. Normalizing to '\' and running through GetFullPath matters because
            // SaveFileDialog.InitialDirectory (via the shell's IFileDialog) throws
            // ArgumentException on a mixed-separator path like "C:\Photos\AutoNum/test", even
            // though Directory.CreateDirectory tolerates it fine.
            var relativeFolder = configuredFolder.TrimStart('\\', '/').Replace('/', '\\');
            var resolvedFolder = Path.GetFullPath(Path.Combine(sourcePath, relativeFolder));
            Directory.CreateDirectory(resolvedFolder);
            return resolvedFolder;
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
        private RelayCommand? _saveCommand;
        private RelayCommand? _saveAsCommand;
    }
}
