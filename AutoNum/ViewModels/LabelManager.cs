using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using MahApps.Metro.Controls.Dialogs;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace AutoNumber.ViewModels
{
    public class LabelManager : BaseViewModel
    {
        #region Commands ---------------------------------------------------------

        RelayCommand? _numerateCommand;
        public RelayCommand NumerateCommand => _numerateCommand ??= new RelayCommand(Numerate);
        public void Numerate(object? _ = null)
        {
            if (_imageVM.Persons.Count == 0) return;

            var orderedPersons = _imageVM.Persons
                .OrderBy(person => person.Row <= 0 ? int.MaxValue : person.Row)
                .ThenBy(person => person.Label.X)
                .ToList();

            var nr = 1;
            foreach (var person in orderedPersons)
            {
                person.Label.Number = nr;
                nr++;
            }

            RecalculateBaseLabelFontSize();
            ApplyScale();

            try
            {
                WeakReferenceMessenger.Default.Send(new LabelsChangedMessage());
            }
            catch (Exception ex) { Trace.WriteLine($"Numerate refresh failed: {ex}"); }
        }

        RelayCommand? _deleteAllLabelsCommand;
        public RelayCommand DeleteAllLabelsCommand => _deleteAllLabelsCommand ??= new RelayCommand(DeleteAllLabelsAsync);
        public async void DeleteAllLabelsAsync(object? _ = null)
        {
            if (_imageVM.Persons.Count == 0) return;

            if (!await ConfirmDeleteNamesIfNeededAsync())
            {
                return;
            }

            // Clear all persons (labels and names) and reset row state to one row
            _imageVM.Persons.Clear();
            _imageVM.ResetRowState();

            try
            {
                WeakReferenceMessenger.Default.Send(new LabelsChangedMessage());
            }
            catch (Exception ex) { Trace.WriteLine($"DeleteAllLabels refresh failed: {ex}"); }
        }

        RelayCommand? _redetectFacesCommand;
        public RelayCommand RedetectFacesCommand => _redetectFacesCommand ??= new RelayCommand(RedetectFacesAsync);
        public async void RedetectFacesAsync(object? _ = null)
        {
            if (_imageVM.Bitmap is null) return;

            // Check if there are any names in the list
            bool hasNames = _imageVM.Persons.Any(p => !string.IsNullOrEmpty(p.Name.Text));

            if (hasNames && _dialogCoordinator != null && _mainVM != null)
            {
                var settings = new MahApps.Metro.Controls.Dialogs.MetroDialogSettings
                {
                    AffirmativeButtonText = "Neu erkennen",
                    NegativeButtonText = "Abbrechen",
                    DefaultButtonFocus = MahApps.Metro.Controls.Dialogs.MessageDialogResult.Negative
                };

                var result = await _dialogCoordinator.ShowMessageAsync(
                    _mainVM,
                    "Gesichter neu erkennen?",
                    "Es wurden Namen eingetragen. Wenn Sie Gesichter neu erkennen, werden alle bisherigen Labels und Namen gelöscht. Fortfahren?",
                    MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegative,
                    settings);

                if (result != MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative)
                    return;
            }

            // Clear all persons
            _imageVM.Persons.Clear();

            // Re-run face detection
            var faces = FaceDetector.Detect(_imageVM.Bitmap);
            Trace.WriteLine($"RedetectFaces: detected {faces.Count} face(s)");

            // Set labels with newly detected faces (scale is preserved as it's not modified by SetLabels)
            SetLabels(faces, _mainVM?.SettingsManager.RowDetectionEnabled ?? true);

            try
            {
                WeakReferenceMessenger.Default.Send(new LabelsChangedMessage());
            }
            catch (Exception ex) { Trace.WriteLine($"RedetectFaces refresh failed: {ex}"); }
        }

        RelayCommand? _rotateImageCommand;
        public RelayCommand RotateImageCommand => _rotateImageCommand ??= new RelayCommand(RotateImageAsync);
        public async void RotateImageAsync(object? _ = null)
        {
            if (_imageVM.Bitmap is null)
            {
                return;
            }

            if (!await ConfirmDeleteNamesIfNeededAsync())
            {
                return;
            }

            _imageVM.Persons.Clear();

            var rotatedBitmap = (Bitmap)_imageVM.Bitmap.Clone();
            rotatedBitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
            _imageVM.Bitmap = rotatedBitmap;
            _imageVM.Init();

            var faces = FaceDetector.Detect(_imageVM.Bitmap);
            Trace.WriteLine($"RotateImage: detected {faces.Count} face(s) after rotation");
            SetLabels(faces, _mainVM?.SettingsManager.RowDetectionEnabled ?? true);

            try
            {
                WeakReferenceMessenger.Default.Send(new LabelsChangedMessage());
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"RotateImage refresh failed: {ex}");
            }
        }

        private async Task<bool> ConfirmDeleteNamesIfNeededAsync()
        {
            var hasNames = _imageVM.Persons.Any(p => !string.IsNullOrEmpty(p.Name.Text));
            if (!hasNames || _dialogCoordinator is null || _mainVM is null)
            {
                return true;
            }

            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Löschen",
                NegativeButtonText = "Abbrechen",
                DefaultButtonFocus = MessageDialogResult.Negative
            };

            var result = await _dialogCoordinator.ShowMessageAsync(
                _mainVM,
                "Labels löschen?",
                "Es wurden Namen eingetragen. Wenn Sie alle Labels löschen, werden auch die zugehörigen Namen gelöscht. Fortfahren?",
                MessageDialogStyle.AffirmativeAndNegative,
                settings);

            return result == MessageDialogResult.Affirmative;
        }

        #endregion
        #region Properties --------------------------------------------------

        /// <summary>
        /// Label scale factor (0.25–4.0). Model property that drives label diameter and font size.
        /// </summary>
        public double LabelScale
        {
            get => _labelScale;
            set
            {
                var clamped = Math.Clamp(value, 0.25, 4.0);
                SetProperty(ref _labelScale, clamped);
                ApplyScale();
            }
        }
        public Color FontColor
        {
            get => _fontColor;
            set
            {
                SetProperty(ref _fontColor, value);
                MarkerLabel.Style.FontColor = _fontColor;
            }
        }
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                SetProperty(ref _backgroundColor, value);
                MarkerLabel.Style.BackgroundColor = value;
            }
        }
        public Color EdgeColor
        {
            get => _edgeColor;
            set
            {
                SetProperty(ref _edgeColor, value);
                MarkerLabel.Style.EdgeColor = value;
            }
        }

        #endregion
        public void SetLabels(List<Rectangle> faces, bool assignRows = true)
        {
            BaseLabelDiameter = SizingModel.ComputeBaseLabelDiameter(faces, _imageVM.Bitmap?.Width ?? 0, _imageVM.Bitmap?.Height ?? 0);

            var anchor = _mainVM?.SettingsManager.FaceLabelAnchor ?? FaceLabelAnchor.BottomCenter;
            var newPersons = faces.Select(face => new Person(0, "", ResolveAnchorPosition(face, anchor)));
            _imageVM.Persons.AddRange(newPersons);

            if (assignRows)
            {
                AssignDetectedRows();
            }

            RecalculateBaseLabelFontSize();
            RecalculateBaseTextFontSize();
            Numerate();
            CalculateAndStoreRowBoundaries();
        }

        /// <summary>
        /// Resolves the label's initial center point within a detected face rectangle
        /// for the given anchor. BottomCenter keeps the historical slight overshoot
        /// below the chin; all other anchors sit exactly on the rectangle's fraction point.
        /// </summary>
        private static PointF ResolveAnchorPosition(Rectangle face, FaceLabelAnchor anchor)
        {
            if (anchor == FaceLabelAnchor.BottomCenter)
            {
                return new PointF(face.X + face.Width / 2f, face.Y + face.Height * 1.05f);
            }

            var (fracX, fracY) = anchor.ToFraction();
            return new PointF(
                (float)(face.X + fracX * face.Width),
                (float)(face.Y + fracY * face.Height));
        }

        public void AssignDetectedRows()
        {
            if (_imageVM.Persons.Count == 0)
            {
                return;
            }

            var persons = _imageVM.Persons.ToList();
            var anchors = persons
                .Select(person => new { Person = person, Y = (double)person.GetRowAnchorPoint().Y })
                .ToList();

            var minY = anchors.Min(item => item.Y);
            var maxY = anchors.Max(item => item.Y);
            var span = Math.Max(0, maxY - minY);
            var estimatedRowCount = (int)Math.Max(1, span / (BaseLabelDiameter * 1.25));

            if (span <= 0 || estimatedRowCount <= 1)
            {
                foreach (var person in persons)
                {
                    person.Row = 1;
                }

                return;
            }

            var delta = span / estimatedRowCount;

            foreach (var item in anchors)
            {
                var normalized = (item.Y - minY) / delta;
                var detectedRow = Math.Clamp((int)Math.Floor(normalized) + 1, 1, estimatedRowCount);
                item.Person.Row = detectedRow;
            }

            var rowMap = persons
                .Select(person => person.Row)
                .Where(row => row > 0)
                .Distinct()
                .OrderBy(row => row)
                .Select((row, index) => new { row, compactRow = index + 1 })
                .ToDictionary(item => item.row, item => item.compactRow);

            foreach (var person in persons)
            {
                person.Row = rowMap.TryGetValue(person.Row, out var compactRow) ? compactRow : 1;
            }
        }

        /// <summary>
        /// Calculate row boundaries from the currently assigned row numbers and store them in metadata.
        /// Only creates boundaries for multi-row images (single-row images get empty boundaries).
        /// </summary>
        public void CalculateAndStoreRowBoundaries()
        {
            if (_imageVM.Persons.Count == 0 || _imageVM.CurrentMetadata is null)
            {
                return;
            }

            // Group persons by row
            var groups = _imageVM.Persons
                .Where(person => person.Row > 0)
                .GroupBy(person => person.Row)
                .OrderBy(group => group.Key)
                .Select(group => new
                {
                    Row = group.Key,
                    MinY = group.Min(person => (double)person.GetRowAnchorPoint().Y),
                    MaxY = group.Max(person => (double)person.GetRowAnchorPoint().Y)
                })
                .ToList();

            // Single row or no rows: no boundaries needed
            if (groups.Count <= 1)
            {
                _imageVM.CurrentMetadata.RowBoundaries = [];
                _imageVM.CurrentMetadata.RowCount = 1;
                return;
            }

            // Multi-row: calculate boundaries between rows
            var boundaries = new List<RowBoundary>();
            for (var index = 0; index < groups.Count - 1; index++)
            {
                var current = groups[index];
                var next = groups[index + 1];
                var boundaryY = (current.MaxY + next.MinY) / 2.0;

                var minAllowed = index == 0 ? 0.0 : boundaries[index - 1].LeftY + 2.0;
                boundaryY = Math.Clamp(boundaryY, minAllowed, _imageVM.ImageHeight);

                boundaries.Add(new RowBoundary(boundaryY, boundaryY));
            }

            _imageVM.CurrentMetadata.RowBoundaries = boundaries;
            _imageVM.CurrentMetadata.RowCount = groups.Count;
        }

        public LabelManager(ImageVM imageVM)
        {
            _imageVM = imageVM;
            BackgroundColor = Color.White;
            FontColor = Color.Black;
            EdgeColor = Color.Black;

            WeakReferenceMessenger.Default.Register<NewImageOpenedMessage>(this, (r, msg) =>
            {
                MarkerLabel.Style.BackgroundColor = BackgroundColor;
                MarkerLabel.Style.EdgeColor = EdgeColor;
                MarkerLabel.Style.FontColor = FontColor;
                SetLabels(msg.Faces, _mainVM?.SettingsManager.RowDetectionEnabled ?? true);
            });

            WeakReferenceMessenger.Default.Register<LabelsChangedMessage>(this, (r, msg) =>
            {
                ApplyScale();
            });

            WeakReferenceMessenger.Default.Register<MetadataLoadedMessage>(this, (r, msg) =>
            {
                try
                {
                    var md = msg.Metadata;
                    Trace.WriteLine($"MetadataLoaded[LabelManager]: start version={md.Version}, labels={md.Persons.Count}");

                    BackgroundColor = Color.FromArgb(md.LabelsFont.Background);
                    FontColor = Color.FromArgb(md.LabelsFont.Foreground);
                    MarkerLabel.Style.FontFamily = FontFamilyResolver.Resolve(md.LabelsFont.Family, MarkerLabel.Style.FontFamily);

                    var v3 = md as AutoNumMetaData_V3;

                    if (v3 is not null && double.IsFinite(v3.BaseLabelDiameter) && v3.BaseLabelDiameter > 0)
                    {
                        BaseLabelDiameter = v3.BaseLabelDiameter;
                        BaseLabelFontSize = double.IsFinite(v3.BaseLabelFontSize) && v3.BaseLabelFontSize > 0
                            ? v3.BaseLabelFontSize
                            : md.LabelsFont.Size;
                        LabelScale = v3.LabelScale;
                        Trace.WriteLine($"MetadataLoaded[LabelManager]: V3+ branch baseDiameter={BaseLabelDiameter:F4}, baseFont={BaseLabelFontSize:F4}, labelScale={LabelScale:F4}, visibleDiameter={MarkerLabel.Style.Diameter:F4}, visibleFont={MarkerLabel.Style.FontSize:F4}");
                    }
                    else
                    {
                        BaseLabelDiameter = double.IsFinite(md.LabelsSize) && md.LabelsSize > 0
                            ? md.LabelsSize
                            : Math.Max(1, md.LabelsFont.Size * 0.95);
                        BaseLabelFontSize = double.IsFinite(md.LabelsFont.Size) && md.LabelsFont.Size > 0
                            ? SizingModel.LegacyStoredFontSizeToVisibleSize(md.LabelsFont.Size)
                            : SizingModel.ComputeFittedLabelFontSize(BaseLabelDiameter, _imageVM.Persons);
                        LabelScale = 1.0;
                        Trace.WriteLine($"MetadataLoaded[LabelManager]: legacy branch labelsSize={md.LabelsSize:F4}, labelsFontStored={md.LabelsFont.Size:F4}, baseDiameter={BaseLabelDiameter:F4}, baseFont={BaseLabelFontSize:F4}, labelScale={LabelScale:F4}, visibleDiameter={MarkerLabel.Style.Diameter:F4}, visibleFont={MarkerLabel.Style.FontSize:F4}");
                    }

                    if (v3 is not null && double.IsFinite(v3.BaseTextFontSize) && v3.BaseTextFontSize > 0)
                    {
                        BaseTextFontSize = v3.BaseTextFontSize;
                        Trace.WriteLine($"MetadataLoaded[LabelManager]: restored baseTextFont={BaseTextFontSize:F4}");
                    }
                    else
                    {
                        // Pre-dates BaseTextFontSize persistence (or legacy metadata) — recompute
                        // fresh from the restored image's dimensions.
                        RecalculateBaseTextFontSize();
                        Trace.WriteLine($"MetadataLoaded[LabelManager]: recomputed baseTextFont={BaseTextFontSize:F4} (no stored value)");
                    }

                    WeakReferenceMessenger.Default.Send(new LabelsChangedMessage());
                    Trace.WriteLine($"MetadataLoaded[LabelManager]: post-refresh visibleDiameter={MarkerLabel.Style.Diameter:F4}, visibleFont={MarkerLabel.Style.FontSize:F4}");
                    Trace.WriteLine("MetadataLoaded[LabelManager]: completed");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"MetadataLoaded[LabelManager]: failed - {ex}");
                    throw;
                }
            });
        }

        public double BaseLabelDiameter { get; private set; } = 50;
        public double BaseLabelFontSize { get; private set; } = 12;

        /// <summary>
        /// 100% baseline for Namensliste/Title/Description/Image-ID (not the label numbers
        /// themselves, see BaseLabelFontSize). See SizingModel.ComputeBaseTextFontSize.
        /// </summary>
        public double BaseTextFontSize { get; private set; } = 12;

        private void RecalculateBaseLabelFontSize()
        {
            BaseLabelFontSize = SizingModel.ComputeFittedLabelFontSize(BaseLabelDiameter, _imageVM.Persons);
        }

        private void RecalculateBaseTextFontSize()
        {
            BaseTextFontSize = SizingModel.ComputeBaseTextFontSize(_imageVM.Bitmap?.Width ?? 0, _imageVM.Bitmap?.Height ?? 0);
        }

        private void ApplyScale()
        {
            if (BaseLabelDiameter <= 0)
            {
                return;
            }

            var actualDiameter = SizingModel.ResolveSize(BaseLabelDiameter, LabelScale);
            var actualFontSize = SizingModel.ResolveSize(BaseLabelFontSize, LabelScale);

            MarkerLabel.Style.Diameter = (float)actualDiameter;
            MarkerLabel.Style.FontSize = actualFontSize;
            _imageVM.LabelDiameter = actualDiameter;
        }

        private readonly ImageVM _imageVM;
        private Color _edgeColor, _fontColor, _backgroundColor;
        private double _labelScale = 1.0; // Default scale

        // Set by MainVM after creation
        internal IDialogCoordinator? _dialogCoordinator { get; set; }
        internal MainVM? _mainVM { get; set; }
    }
}
