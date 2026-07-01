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

        public AutoNumMetaData_V4? CurrentMetadata
        {
            get => _currentMetadata;
            private set => SetProperty(ref _currentMetadata, value);
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

            // Upgrade older versions to V4 and store as current metadata
            if (md is AutoNumMetaData_V4 v4)
            {
                CurrentMetadata = v4;
            }
            else
            {
                // Upgrade to V4 - copy all V1/V2/V3 properties into a new V4 instance
                CurrentMetadata = new AutoNumMetaData_V4
                {
                    Version = "V4",
                    Created = md.Created,
                    Creator = md.Creator,
                    OriginalImage = md.OriginalImage,
                    AutoNumImage = md.AutoNumImage,
                    LabelsFont = md.LabelsFont,
                    LabelsSize = md.LabelsSize,
                    NamesFont = md.NamesFont,
                    NamesEnabled = md.NamesEnabled,
                    NamesColumnCount = md.NamesColumnCount,
                    ImageId = md.ImageId,
                    ImageIdFont = md.ImageIdFont,
                    ImageIdEnabled = md.ImageIdEnabled,
                    TitleFont = md.TitleFont,
                    TitleEnabled = md.TitleEnabled,
                    Title = md.Title,
                    ImageInfoFont = md.ImageInfoFont,
                    ImageInfoEnabled = md.ImageInfoEnabled,
                    ImageInfo = md.ImageInfo,
                    Persons = md.Persons,
                    RowCount = 1,
                    RowBoundaries = []
                };
            }

            // Don't automatically show row boundaries - they'll be restored when user opens the dialog
            // The metadata is preserved in CurrentMetadata for later use
            ClearRowDefinition();

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

        public void SyncRowDefinitionToMetadata()
        {
            if (CurrentMetadata == null || RowDefinitionSession == null)
            {
                return;
            }

            // Directly update metadata with current session boundaries
            CurrentMetadata.RowCount = RowDefinitionSession.RowCount;
            CurrentMetadata.RowBoundaries = RowDefinitionSession.Boundaries
                .Select(b => new RowBoundary(b.LeftY, b.RightY))
                .ToList();
        }

        public void RestoreRowDefinitionFromMetadata()
        {
            if (CurrentMetadata == null || CurrentMetadata.RowBoundaries.Count == 0)
            {
                return;
            }

            var session = new RowDefinitionSession();
            session.Restore(CurrentMetadata.RowCount, ImageWidth, ImageHeight, CurrentMetadata.RowBoundaries);
            RowDefinitionSession = session;
        }

        public void UpdateMetadataBeforeSave(LabelManager lm, NameManager nm, TitleManager tm, ImageInfoManager iim, ImageIdManager idm)
        {
            if (CurrentMetadata == null)
            {
                // Create fresh metadata if none exists (e.g., new image)
                CurrentMetadata = new AutoNumMetaData_V4
                {
                    Version = "V4",
                    Created = DateTime.Now,
                    RowCount = 1,
                    RowBoundaries = []
                };
            }

            // Update all runtime values
            CurrentMetadata.OriginalImage = OriginalImageFilename;
            CurrentMetadata.AutoNumImage = string.Empty;
            CurrentMetadata.Title = tm.Title;
            CurrentMetadata.ImageInfo = iim.ImageInfo;

            CurrentMetadata.LabelsFont = new AutoNumFont(MarkerLabel.Style.FontColor, lm.BackgroundColor, MarkerLabel.Style.FontFamily.Name, MarkerLabel.Style.FontSize);
            CurrentMetadata.LabelsSize = MarkerLabel.Style.Diameter;
            CurrentMetadata.NamesFont = new AutoNumFont(nm.FontColor, nm.BackgroundColor, nm.FontFamily.Name, TextLabel.Style.FontSize);
            CurrentMetadata.NamesEnabled = nm.IsEnabled;
            CurrentMetadata.NamesColumnCount = nm.NameTableColumnCount;
            CurrentMetadata.ImageId = idm.ImageId;
            CurrentMetadata.ImageIdFont = new AutoNumFont(idm.FontColor, idm.BackgroundColor, idm.FontFamily.Name, idm.FontSize);
            CurrentMetadata.ImageIdEnabled = idm.IsEnabled;
            CurrentMetadata.TitleFont = new AutoNumFont(tm.TitleFontColor, tm.BackgroundColor, tm.TitleFontFamily.Name, tm.TitleFontSize);
            CurrentMetadata.TitleEnabled = tm.IsEnabled;
            CurrentMetadata.ImageInfoFont = new AutoNumFont(iim.ImageInfoFontColor, iim.BackgroundColor, iim.ImageInfoFontFamily.Name, iim.ImageInfoFontSize);
            CurrentMetadata.ImageInfoEnabled = iim.IsEnabled;

            // Update persons
            CurrentMetadata.Persons.Clear();
            foreach (var person in Persons)
            {
                CurrentMetadata.Persons.Add(new AutoNumPerson(person));
            }

            // Row boundaries are synced directly from session if active
            if (RowDefinitionSession != null)
            {
                CurrentMetadata.RowCount = RowDefinitionSession.RowCount;
                CurrentMetadata.RowBoundaries = RowDefinitionSession.Boundaries
                    .Select(b => new RowBoundary(b.LeftY, b.RightY))
                    .ToList();
            }
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

            // Reset metadata for a fresh image with no embedded metadata
            CurrentMetadata = new AutoNumMetaData_V4
            {
                Version = "V4",
                RowCount = 1,
                RowBoundaries = []
            };

            // Clear any active row definition session
            ClearRowDefinition();

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
        private AutoNumMetaData_V4? _currentMetadata;
    }
}
