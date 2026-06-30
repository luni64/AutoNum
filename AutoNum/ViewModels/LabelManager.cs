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
            var persons = _imageVM.Persons;

            double minY = persons.Min(p => p.Label.CenterY);
            double maxY = persons.Max(p => p.Label.CenterY);
            int nrOfRows = (int)Math.Max(1, (maxY - minY) / (BaseLabelDiameter * 1.25));
            double delta = (maxY - minY) / nrOfRows;

            int nr = 1;
            for (int row = 0; row < nrOfRows; row++)
            {
                double lower = minY + row * delta;
                double upper = minY + (row + 1) * delta;
                foreach (var person in persons.Where(p => p.Label.Y >= lower && p.Label.Y <= upper).OrderBy(p => p.Label.X))
                {
                    person.Label.Number = nr;
                    nr++;
                }
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

            // Clear all persons (labels and names)
            _imageVM.Persons.Clear();

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
            SetLabels(faces);

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
            SetLabels(faces);

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
        public void SetLabels(List<Rectangle> faces)
        {
            BaseLabelDiameter = SizingModel.ComputeBaseLabelDiameter(faces, _imageVM.Bitmap?.Width ?? 0);

            foreach (var face in faces)
            {
                PointF labelPos = new PointF((float)(face.X + face.Width / 2), (float)(face.Y + face.Height * 1.05));
                _imageVM.Persons.Add(new Person(0, "", labelPos));
            }

            RecalculateBaseLabelFontSize();
            Numerate();
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
                SetLabels(msg.Faces);
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

                    if (md is AutoNumMetaData_V3 v3 && double.IsFinite(v3.BaseLabelDiameter) && v3.BaseLabelDiameter > 0)
                    {
                        BaseLabelDiameter = v3.BaseLabelDiameter;
                        BaseLabelFontSize = double.IsFinite(v3.BaseLabelFontSize) && v3.BaseLabelFontSize > 0
                            ? v3.BaseLabelFontSize
                            : md.LabelsFont.Size;
                        LabelScale = v3.LabelScale;
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
                    }

                    WeakReferenceMessenger.Default.Send(new LabelsChangedMessage());
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

        private void RecalculateBaseLabelFontSize()
        {
            BaseLabelFontSize = SizingModel.ComputeFittedLabelFontSize(BaseLabelDiameter, _imageVM.Persons);
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
