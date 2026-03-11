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
            if (_imageModel.Persons.Count == 0) return;
            var persons = _imageModel.Persons;

            double minY = persons.Min(p => p.Label.CenterY);
            double maxY = persons.Max(p => p.Label.CenterY);
            int nrOfRows = (int)Math.Max(1, (maxY - minY) / (DefaultDiameter * 1.25));
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
            try
            {
                WeakReferenceMessenger.Default.Send(new LabelsChangedMessage());
            }
            catch (Exception ex) { Trace.WriteLine($"Numerate refresh failed: {ex}"); }
        }



        #endregion
        #region Properties --------------------------------------------------

        public int Diameter  // is attached to a slider 0...100. slider values: 0 -> 0.5*d_0,  50 => d_0,  100 => 2*d_0
        {
            get => _diameter;
            set
            {
                SetProperty(ref _diameter, value);
                _imageModel.LabelDiameter = DefaultDiameter * (0.5 + 0.0002 * (_diameter * _diameter));
            }
        }
        public Color FontColor
        {
            get => _fontColor;
            set
            {
                SetProperty(ref _fontColor, value);
                MarkerLabel.FontColor = _fontColor;
            }
        }
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                SetProperty(ref _backgroundColor, value);
                MarkerLabel.BackgroundColor = value;
            }
        }
        public Color EdgeColor
        {
            get => _edgeColor;
            set
            {
                SetProperty(ref _edgeColor, value);
                MarkerLabel.EdgeColor = value;
            }
        }

        #endregion
        public void SetLabels(List<Rectangle> faces)
        {
            if (faces.Count > 0)
            {
                DefaultDiameter = Math.Max(faces.Average(m => m.Width), faces.Average(m => m.Height)) / 2;
            }
            else DefaultDiameter = _imageModel.Bitmap?.Width / 20 ?? 50;

            foreach (var face in faces)
            {
                PointF labelPos = new PointF((float)(face.X + face.Width / 2), (float)(face.Y + face.Height * 1.05));               
                _imageModel.Persons.Add(new Person(0, "", labelPos));
            }

            Numerate();

            Diameter = 0;
            Diameter = 50; // slider value 
        }

        public LabelManager(ImageModel imageModel)
        {
            _imageModel = imageModel;
            BackgroundColor = Color.White;
            FontColor = Color.Black;
            EdgeColor = Color.Black;

            WeakReferenceMessenger.Default.Register<NewImageOpenedMessage>(this, (r, msg) =>
            {
                MarkerLabel.BackgroundColor = BackgroundColor;
                MarkerLabel.EdgeColor = EdgeColor;
                MarkerLabel.FontColor = FontColor;
                MarkerLabel.FontSize = 12;
                SetLabels(msg.Faces);
            });

            WeakReferenceMessenger.Default.Register<MetadataLoadedMessage>(this, (r, msg) =>
            {
                var md = msg.Metadata;
                BackgroundColor = Color.FromArgb(md.LabelsFont.Background);
                FontColor = Color.FromArgb(md.LabelsFont.Foreground);
                var labelSize = double.IsFinite(md.LabelsSize) ? md.LabelsSize : md.LabelsFont.Size * 0.95;
                DefaultDiameter = labelSize;
                Diameter = 0;
                Diameter = 50;
            });
        }

        public double DefaultDiameter { get; set; } = 50;

        private readonly ImageModel _imageModel;
        private Color _edgeColor, _fontColor, _backgroundColor;
        private int _diameter;
    }
}
