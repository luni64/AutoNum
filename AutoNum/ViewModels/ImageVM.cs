using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Input;


namespace AutoNumber.ViewModels
{
    public class ImageVM : BaseViewModel, IDisposable
    {

        #region Properties ----------------------------------------------------
        public Bitmap? Bitmap
        {
            get => _bitmap;
            set
            {
                if (_bitmap != value)
                    _bitmap?.Dispose();
                SetProperty(ref _bitmap, value);
            }
        }
        // Path of the true source photo that must not be overwritten.
        public string OriginalImageFilename { get; set; } = string.Empty;

        // Path of the currently opened/edited image (used for save suggestions).
        public string CurrentImageFilename
        {
            get => _currentImageFilename;
            set => SetProperty(ref _currentImageFilename, value);
        }
        private string _currentImageFilename = string.Empty;

        public PropertyItem[]? OriginalPropertyItems { get; set; }
        public ObservableCollection<Person> Persons { get; } = [];
        public double LabelDiameter
        {
            get => _labelDiameter;
            set
            {
                if (_labelDiameter != value)
                {
                    MarkerLabel.Style.Diameter = (float)value;
                    SetProperty(ref _labelDiameter, value);
                }
            }
        }
        public Size CanvasSize { get; set; }
        public int ImageWidth
        {
            get => _imageWidth;
            set => SetProperty(ref _imageWidth, value);
        }
        public int ImageHeight
        {
            get => _imageHeight;
            set => SetProperty(ref _imageHeight, value);
        }
        public double NamesRegionHeight
        {
            get => _namesRegionHeight;
            set => SetProperty(ref _namesRegionHeight, value);
        }
        public double TitleRegionHeight
        {
            get => _titleRegionHeight;
            set => SetProperty(ref _titleRegionHeight, value);
        }
        public RowDefinitionSession? RowDefinitionSession
        {
            get => _rowDefinitionSession;
            private set => SetProperty(ref _rowDefinitionSession, value);
        }

        // position and size of the image on the canvas
        public int PanX
        {
            get => _panX;
            set => SetProperty(ref _panX, value);
        }
        public int PanY
        {
            get => _panY;
            set => SetProperty(ref _panY, value);
        }
        public double Zoom
        {
            get => _zoom;
            set => SetProperty(ref _zoom, value);
        }

        public ICommand ZoomToFitCommand { get; }

        #endregion

        public ImageVM()
        {
            ZoomToFitCommand = new RelayCommand(_ => FitToCanvas(), _ => CanFitToCanvas());
        }

        public void InitFromMetadata(AutoNumMetaData_V1 md)
        {
            ImageWidth = Bitmap?.Width ?? 0;
            ImageHeight = Bitmap?.Height ?? 0;
            FitToCanvas();

            Persons.Clear();

            foreach (var p in md.Persons)
            {
                this.Persons.Add(new Person(p.Label.Number, p.Name.Text, new PointF(p.Label.CenterX, p.Label.CenterY))
                {
                    Row = p.Row
                });
            }

            var labelSize = double.IsFinite(md.LabelsSize) ? md.LabelsSize : md.LabelsFont.Size * 0.95;
            LabelDiameter = labelSize;

            if (md is AutoNumMetaData_V4 v4)
            {
                if (v4.RowBoundaries.Count > 0)
                {
                    BeginRowDefinition(Math.Max(1, v4.RowCount));
                    RowDefinitionSession?.Restore(
                        Math.Max(1, v4.RowCount),
                        ImageWidth,
                        ImageHeight,
                        v4.RowBoundaries);
                }
                else
                {
                    ClearRowDefinition();
                }
            }
            else
            {
                ClearRowDefinition();
            }

            Trace.WriteLine($"InitFromMetadata: sending MetadataLoadedMessage version={md.Version}, persons={md.Persons.Count}");
            WeakReferenceMessenger.Default.Send(new MetadataLoadedMessage(md));
            Trace.WriteLine("InitFromMetadata: MetadataLoadedMessage completed");
        }

        public void BeginRowDefinition(int rowCount)
        {
            var session = new RowDefinitionSession();
            session.Initialize(rowCount, ImageWidth, ImageHeight);
            RowDefinitionSession = session;
        }

        public void ClearRowDefinition()
        {
            RowDefinitionSession = null;
        }

        public void SaveRowDefinitionToMetadata()
        {
            if (RowDefinitionSession is null)
            {
                return;
            }

            // Store the current row boundaries in memory for later restoration
            _savedRowCount = RowDefinitionSession.RowCount;
            _savedRowBoundaries = RowDefinitionSession.Boundaries
                .Select(b => new RowBoundary(b.LeftY, b.RightY))
                .ToList();
        }

        public void RestoreRowDefinitionFromSavedState()
        {
            if (_savedRowBoundaries.Count == 0)
            {
                return;
            }

            var session = new RowDefinitionSession();
            session.Restore(_savedRowCount, ImageWidth, ImageHeight, _savedRowBoundaries);
            RowDefinitionSession = session;
        }

        public (int rowCount, List<RowBoundary> boundaries) GetSavedRowDefinitionState()
        {
            return (_savedRowCount, _savedRowBoundaries);
        }

        public void ApplyRowDefinition()
        {
            RowDefinitionSession?.ApplyToPersons(Persons);
        }

        public void Init()
        {
            Persons.Clear();

            ImageWidth = Bitmap?.Width ?? 0;
            ImageHeight = Bitmap?.Height ?? 0;

            FitToCanvas();
        }

        private bool CanFitToCanvas() => ImageWidth > 0 && ImageHeight > 0 && CanvasSize.Width > 0 && CanvasSize.Height > 0;

        private void FitToCanvas()
        {
            if (!CanFitToCanvas())
            {
                return;
            }

            Zoom = 0.95 * Math.Min((double)CanvasSize.Width / ImageWidth, (double)CanvasSize.Height / ImageHeight);
            PanX = (int)((CanvasSize.Width - ImageWidth * Zoom) / 2);
            PanY = (int)((CanvasSize.Height - ImageHeight * Zoom) / 2);
        }

        public void Dispose() => Bitmap?.Dispose();

        private Bitmap? _bitmap;
        private int _imageHeight, _imageWidth;
        private int _panX, _panY;
        private double _zoom = 0.7;
        private double _labelDiameter;
        private double _namesRegionHeight;
        private double _titleRegionHeight = 300;
        private RowDefinitionSession? _rowDefinitionSession;
        private int _savedRowCount = 0;
        private List<RowBoundary> _savedRowBoundaries = [];
    }
}
