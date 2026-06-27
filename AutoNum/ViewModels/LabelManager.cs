using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;
using System.Drawing;

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
            LabelScale = 1.0; // Default scale
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
                var md = msg.Metadata;
                BackgroundColor = Color.FromArgb(md.LabelsFont.Background);
                FontColor = Color.FromArgb(md.LabelsFont.Foreground);

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
                    LabelScale = 1.0; // Default scale
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
    }
}
