using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using System.Diagnostics;
using System.Drawing;
using DrawingImageFormat = System.Drawing.Imaging.ImageFormat;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

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
                    Trace.WriteLine("OpenImage: no AutoNum metadata, running face detection");
                    var faces = FaceDetector.Detect(bitmap);
                    pvm.OriginalImageFilename = filename;
                    pvm.CurrentImageFilename = filename;
                    pvm.OriginalPropertyItems = bitmap.PropertyItems;
                    pvm.Bitmap = bitmap;
                    pvm.Init();
                    WeakReferenceMessenger.Default.Send(new NewImageOpenedMessage(faces));
                    parent.SettingsManager.ApplyFreshImageDefaults(parent.LabelManager, parent.NameManager, parent.TitleManager, parent.ImageInfoManager, parent.ImageIdManager);
                    Trace.WriteLine($"OpenImage: fresh image initialized with {faces.Count} detected face(s)");
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
            catch (InvalidOperationException) { Trace.WriteLine("No faces found"); }
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

        public RelayCommand SaveJpgCommand => _saveJpgCommand ??= new(ExecuteSaveJpg);
        public RelayCommand SavePdfCommand => _savePdfCommand ??= new(ExecuteSavePdf);
        public RelayCommand SaveImageCommand => SaveJpgCommand;

        void ExecuteSaveJpg(object? o)
        {
            var fullFilename = GetCurrentSaveFilename();
            var saveFileInfo = CreateSaveFileInfo(fullFilename, ".jpg", "JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg");

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
                result.Bitmap.Save(jpegStream, DrawingImageFormat.Jpeg);
                var jpegBytes = jpegStream.ToArray();

                var finalBytes = AppSegmentIO.InjectSegments(jpegBytes, result.Patches);
                File.WriteAllBytes(filename, finalBytes);
                parent.PictureVM.CurrentImageFilename = filename;

                var exportData = BuildExportData();
                exportData.GeneratedAt = DateTimeOffset.Now.ToString("O");
                WriteMetadataSidecars(filename, exportData, result);
            }
        }

        void ExecuteSavePdf(object? o)
        {
            var fullFilename = GetCurrentSaveFilename();
            var saveFileInfo = CreateSaveFileInfo(fullFilename, ".pdf", "PDF Files (*.pdf)|*.pdf");

            if (parent.DialogService.ShowDialog(saveFileInfo) is string filename && !string.IsNullOrEmpty(filename))
            {
                if (IsProtectedOriginalPath(filename, parent.PictureVM.OriginalImageFilename))
                {
                    parent.DialogService.ShowDialog("Das Originalbild darf nicht überschrieben werden");
                    return;
                }

                using var result = parent.PictureVM.ToNumberedBitmap(parent.LabelManager, parent.NameManager, parent.TitleManager, parent.ImageInfoManager, parent.ImageIdManager);
                if (result is null) return;

                var exportData = BuildExportData();
                exportData.GeneratedAt = DateTimeOffset.Now.ToString("O");
                WritePdf(filename, exportData, result);
                WriteMetadataSidecars(filename, exportData, result);
                parent.PictureVM.CurrentImageFilename = filename;
            }
        }


        public bool CanExportMetadataNow()
        {
            var hasSelection = ExportCsvMetadata || ExportJsonMetadata;
            var currentFile = GetCurrentSaveFilename();
            return hasSelection && !string.IsNullOrWhiteSpace(currentFile);
        }

        public List<string> ExportMetadataNow()
        {
            var exportedFiles = new List<string>();

            if (!CanExportMetadataNow())
            {
                return exportedFiles;
            }

            var targetFilename = GetCurrentSaveFilename();
            using var result = parent.PictureVM.ToNumberedBitmap(parent.LabelManager, parent.NameManager, parent.TitleManager, parent.ImageInfoManager, parent.ImageIdManager);
            if (result is null)
            {
                return exportedFiles;
            }

            var exportData = BuildExportData();
            exportData.GeneratedAt = DateTimeOffset.Now.ToString("O");
            WriteMetadataSidecars(targetFilename, exportData, result);

            if (ExportCsvMetadata)
            {
                exportedFiles.Add(Path.ChangeExtension(targetFilename, ".csv"));
            }

            if (ExportJsonMetadata)
            {
                exportedFiles.Add(Path.ChangeExtension(targetFilename, ".json"));
            }

            return exportedFiles;
        }

        private SidecarExportData BuildExportData()
        {
            var persons = parent.PictureVM.Persons
                .OrderBy(p => p.Label.Number)
                .Select(p => new SidecarPerson
                {
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
                try
                {
                    var csvPath = Path.ChangeExtension(imageFilename, ".csv");
                    WriteCsv(csvPath, exportData);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error writing CSV sidecar: {ex}");
                }
            }

            if (ExportJsonMetadata)
            {
                try
                {
                    var jsonPath = Path.ChangeExtension(imageFilename, ".json");
                    WriteJson(jsonPath, exportData);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error writing JSON sidecar: {ex}");
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
            builder.AppendLine("Number;Name");

            foreach (var person in exportData.Persons)
            {
                builder.AppendLine($"{person.Number};{EscapeCsv(person.Name)}");
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

        private void WritePdf(string filename, SidecarExportData exportData, NumberedBitmapResult numberedBitmapResult)
        {
            byte[]? photoBytes = null;
            using (var photoWithLabels = parent.PictureVM.ToPhotoWithLabelsBitmap())
            {
                if (photoWithLabels is not null)
                {
                    using var imageStream = new MemoryStream();
                    photoWithLabels.Save(imageStream, DrawingImageFormat.Jpeg);
                    photoBytes = imageStream.ToArray();
                }
            }

            var heading = string.IsNullOrWhiteSpace(exportData.Title) ? "Ohne Titel" : exportData.Title;
            var hasId = !string.IsNullOrWhiteSpace(exportData.Id);
            var hasDescription = !string.IsNullOrWhiteSpace(exportData.Description);
            var namesColumnCount = Math.Clamp(parent.NameManager.NameTableColumnCount, 1, 4);
            var tableReferenceWidth = 360d / namesColumnCount;
            var pdfNumberColumnWidth = NamesTableLayout.ResolveNumberColumnWidth(tableReferenceWidth);

            var createdDate = DateTimeOffset.TryParse(exportData.GeneratedAt, out var generatedAt)
                ? generatedAt.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"))
                : DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));

            using var pdfStream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(10));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().AlignRight().Text(createdDate);

                        column.Item().Text(heading).FontSize(20).SemiBold();

                        if (hasId)
                        {
                            column.Item().PaddingTop(12).Text($"Bild-ID: {exportData.Id}").FontSize(11);
                        }

                        if (hasDescription)
                        {
                            column.Item().PaddingTop(8).Text("Beschreibung").FontSize(13).SemiBold();
                            column.Item().Text(exportData.Description);
                        }

                        if (photoBytes is not null)
                        {
                            const float maxImageWidth = 428f;
                            const float maxImageHeight = 320f;

                            column.Item()
                                .PaddingTop(8)
                                .AlignCenter()
                                .MaxWidth(maxImageWidth)
                                .MaxHeight(maxImageHeight)
                                .Image(photoBytes)
                                .FitArea();
                        }

                        column.Item().PaddingTop(8).Text("Namensliste").FontSize(14).SemiBold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                for (var c = 0; c < namesColumnCount; c++)
                                {
                                    cols.ConstantColumn(pdfNumberColumnWidth);
                                    cols.RelativeColumn();
                                }
                            });

                            table.Header(header =>
                            {
                                for (var c = 0; c < namesColumnCount; c++)
                                {
                                    header.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text("Nummer").SemiBold();
                                    header.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text("Name").SemiBold();
                                }
                            });

                            for (var index = 0; index < exportData.Persons.Count; index += namesColumnCount)
                            {
                                for (var c = 0; c < namesColumnCount; c++)
                                {
                                    var personIndex = index + c;
                                    if (personIndex < exportData.Persons.Count)
                                    {
                                        var person = exportData.Persons[personIndex];
                                        table.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text(person.Number.ToString());
                                        table.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text(person.Name);
                                    }
                                    else
                                    {
                                        table.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text(string.Empty);
                                        table.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text(string.Empty);
                                    }
                                }
                            }
                        });
                    });
                });
            }).GeneratePdf(pdfStream);

            var metadata = new AutoNumMetaData_V3(parent.PictureVM, parent.LabelManager, parent.NameManager, parent.TitleManager, parent.ImageInfoManager, parent.ImageIdManager);
            using var compositeStream = new MemoryStream();
            numberedBitmapResult.Bitmap.Save(compositeStream, DrawingImageFormat.Jpeg);

            var payloadZip = PdfPayloadStore.CreatePayloadZip(new PdfPayloadData
            {
                Metadata = metadata,
                CompositeImageBytes = compositeStream.ToArray(),
                Patches = [.. numberedBitmapResult.Patches]
            });

            if (!PdfPayloadStore.TryReadPayloadZip(payloadZip, out var payloadCheck)
                || payloadCheck is null
                || payloadCheck.Metadata is null
                || payloadCheck.Patches.Count != numberedBitmapResult.Patches.Count)
            {
                throw new InvalidDataException("Die PDF-Nutzdaten konnten nicht verifiziert werden.");
            }

            var finalPdfBytes = PdfPayloadStore.EmbedPayload(pdfStream.ToArray(), payloadZip);
            File.WriteAllBytes(filename, finalPdfBytes);
        }

        private void OpenFromPdfFile(string pdfFilename, ImageVM pvm)
        {
            Trace.WriteLine($"OpenFromPdfFile: reading '{pdfFilename}'");
            var pdfBytes = File.ReadAllBytes(pdfFilename);
            if (!PdfPayloadStore.TryExtractPayload(pdfBytes, out var payloadZipBytes) || payloadZipBytes is null)
            {
                throw new InvalidDataException("Die PDF enthält keine editierbaren AutoNum-Daten.");
            }

            Trace.WriteLine($"OpenFromPdfFile: extracted payload zip ({payloadZipBytes.Length} bytes)");
            if (!PdfPayloadStore.TryReadPayloadZip(payloadZipBytes, out var payload) || payload is null)
            {
                throw new InvalidDataException("Die eingebetteten AutoNum-Daten in der PDF sind ungültig.");
            }

            Trace.WriteLine($"OpenFromPdfFile: payload metadata version '{payload.Metadata.Version}', patches={payload.Patches.Count}");

            using var compositeStream = new MemoryStream(payload.CompositeImageBytes);
            using var compositeSource = new Bitmap(compositeStream);
            using var compositeBitmap = new Bitmap(compositeSource);

            Bitmap restoredBitmap;
            if (payload.Metadata is AutoNumMetaData_V2 v2)
            {
                restoredBitmap = compositeBitmap.RestoreFromPatches(v2, payload.Patches);
            }
            else
            {
                restoredBitmap = new Bitmap(compositeBitmap);
            }

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
            };

            filename = parent.DialogService.ShowDialog(info) as string ?? string.Empty;            
            return  !string.IsNullOrEmpty(filename);
        }

        private void RefreshPreviewAfterMetadataLoad(string source)
        {
            Trace.WriteLine($"RefreshPreviewAfterMetadataLoad: source={source}");
            parent.NameManager.Refresh();
            parent.NameManager.ShowNames();
        }

        private SaveFileInfo CreateSaveFileInfo(string fullFilename, string extension, string filter)
        {
            var path = Path.GetDirectoryName(fullFilename)!;
            var file = Path.GetFileNameWithoutExtension(fullFilename);
            var isEditingProtectedOriginal = IsProtectedOriginalPath(fullFilename, parent.PictureVM.OriginalImageFilename);

            var outputFile = isEditingProtectedOriginal && !string.IsNullOrWhiteSpace(parent.SettingsManager.SaveFileSuffix)
                ? file + parent.SettingsManager.SaveFileSuffix + extension
                : file + extension;

            return new SaveFileInfo
            {
                Filename = outputFile,
                InitialDirectory = path,
                Filter = filter,
            };
        }

        private string GetCurrentSaveFilename()
        {
            return !string.IsNullOrWhiteSpace(parent.PictureVM.CurrentImageFilename)
                ? parent.PictureVM.CurrentImageFilename
                : parent.PictureVM.OriginalImageFilename;
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
        private RelayCommand? _saveJpgCommand;
        private RelayCommand? _savePdfCommand;

        private sealed class SidecarExportData
        {
            [JsonPropertyName("generatedAt")]
            public string GeneratedAt { get; set; } = string.Empty;

            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;

            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("persons")]
            public List<SidecarPerson> Persons { get; set; } = [];
        }

        private sealed class SidecarPerson
        {
            [JsonPropertyName("number")]
            public int Number { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
        }
    }
}
