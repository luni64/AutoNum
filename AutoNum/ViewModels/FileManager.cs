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
using QuestPDF.Infrastructure;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

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
                    }

                    parent.SettingsManager.ApplyFreshImageDefaults(parent.LabelManager, parent.NameManager, parent.TitleManager, parent.ImageInfoManager, parent.ImageIdManager);
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
                if (result is null)
                {
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
                if (!WritePdf(filename, exportData))
                {
                    return;
                }

                parent.PictureVM.CurrentImageFilename = filename;
                WriteMetadataSidecars(filename, exportData, result);
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
                    if (!ShowRetryCancelDialog(operationName, filename, ex))
                    {
                        return false;
                    }
                }
            }
        }

        private bool ShowRetryCancelDialog(string operationName, string filename, Exception ex)
        {
            var owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive) ?? Application.Current?.MainWindow;

            var dialog = new Window
            {
                Title = "Speichern fehlgeschlagen",
                Width = 560,
                Height = 420,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Owner = owner,
            };

            var grid = new Grid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = System.Windows.GridLength.Auto });

            var text = new TextBlock
            {
                Text = $"Die {operationName}-Datei konnte nicht gespeichert werden.\n\n" +
                       $"Möglicherweise ist die Datei in einer anderen Anwendung geöffnet.\n\n" +
                       $"Datei: {Path.GetFileName(filename)}\n" +
                       $"Details: {ex.Message}\n\n" +
                       "Schließen Sie die Datei in der anderen Anwendung und klicken Sie auf \"Wiederholen\", oder brechen Sie den Vorgang ab.",
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(text, 0);
            grid.Children.Add(text);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var retryButton = new Button
            {
                Content = "Wiederholen",
                Width = 110,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "Abbrechen",
                Width = 110,
                IsCancel = true
            };

            bool? retrySelected = null;
            retryButton.Click += (_, __) => { retrySelected = true; dialog.DialogResult = true; dialog.Close(); };
            cancelButton.Click += (_, __) => { retrySelected = false; dialog.DialogResult = false; dialog.Close(); };

            buttons.Children.Add(retryButton);
            buttons.Children.Add(cancelButton);
            Grid.SetRow(buttons, 1);
            grid.Children.Add(buttons);

            dialog.Content = grid;
            dialog.ShowDialog();
            return retrySelected == true;
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

            var heading = string.IsNullOrWhiteSpace(exportData.Title) ? "Ohne Titel" : exportData.Title;
            var hasId = !string.IsNullOrWhiteSpace(exportData.Id);
            var hasDescription = !string.IsNullOrWhiteSpace(exportData.Description);
            var namesColumnCount = Math.Clamp(parent.NameManager.NameTableColumnCount, 1, 4);
            var tableReferenceWidth = 360d / namesColumnCount;
            var pdfNumberColumnWidth = Math.Clamp(NamesTableLayout.ResolveNumberColumnWidth(tableReferenceWidth) * 0.5f, 24f, 48f);
            var showRowDividers = parent.NameManager.ShowRowDividers;
            var orderedPersons = exportData.Persons
                .OrderBy(person => person.Row <= 0 ? int.MaxValue : person.Row)
                .ThenBy(person => person.Number)
                .ToList();
            var assignedRowGroups = orderedPersons
                .Where(person => person.Row > 0)
                .GroupBy(person => person.Row)
                .OrderBy(group => group.Key)
                .ToList();
            var unassignedPersons = orderedPersons
                .Where(person => person.Row <= 0)
                .ToList();

            var documentTimestamp = DateTimeOffset.TryParse(exportData.GeneratedAt, out var generatedAt)
                ? generatedAt
                : DateTimeOffset.Now;
            var createdDate = documentTimestamp.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));

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
                                    header.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text("Nr.").SemiBold();
                                    header.Cell().Border(NamesTableLayout.PdfBorderWidth).BorderColor(Colors.Grey.Lighten2).Padding(NamesTableLayout.CellPadding).Text("Name").SemiBold();
                                }
                            });

                            void RenderPersonRows(IReadOnlyList<SidecarPerson> persons)
                            {
                                for (var index = 0; index < persons.Count; index += namesColumnCount)
                                {
                                    for (var c = 0; c < namesColumnCount; c++)
                                    {
                                        var personIndex = index + c;
                                        if (personIndex < persons.Count)
                                        {
                                            var person = persons[personIndex];
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
                            }

                            foreach (var rowGroup in assignedRowGroups)
                            {
                                if (showRowDividers)
                                {
                                    table.Cell()
                                        .ColumnSpan((uint)(namesColumnCount * 2))
                                        .Border(NamesTableLayout.PdfBorderWidth)
                                        .BorderColor(Colors.Grey.Lighten2)
                                        .Padding(NamesTableLayout.CellPadding)
                                        .Text(parent.NameManager.FormatRowDividerText(rowGroup.Key))
                                        .SemiBold();
                                }

                                RenderPersonRows(rowGroup.OrderBy(person => person.Number).ToList());
                            }

                            if (unassignedPersons.Count > 0)
                            {
                                RenderPersonRows(unassignedPersons);
                            }
                        });
                    });
                });
            })
            .WithMetadata(new DocumentMetadata
            {
                Title = heading,
                Author = "AutoNum",
                Subject = "AutoNum export",
                Keywords = "AutoNum, Face labels, Numbered image, PDF",
                Creator = "AutoNum",
                Producer = "QuestPDF",
                Language = "de-DE",
                CreationDate = documentTimestamp,
                ModifiedDate = documentTimestamp
            })
            .GeneratePdf(pdfStream);

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

            if (!TryWriteWithRetry(filename, () => PdfPayloadStore.SavePdfWithPayloadAttachment(pdfStream.ToArray(), payloadZip, filename), "PDF"))
            {
                return false;
            }

            return true;
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
            [JsonPropertyName("row")]
            public int Row { get; set; }

            [JsonPropertyName("number")]
            public int Number { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
        }
    }
}
