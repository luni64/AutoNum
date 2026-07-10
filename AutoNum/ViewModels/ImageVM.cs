using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
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
        public BulkObservableCollection<Person> Persons { get; } = [];
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

        public AutoNumMetaData_V5? CurrentMetadata
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

            var labelSize = double.IsFinite(md.LabelsSize) ? md.LabelsSize : md.LabelsFont.Size * 0.95;

            // Pre-V5 files stored each label's position as the circle's top-left corner
            // rather than its true center; convert using the diameter the file was saved
            // with so previously-saved images/PDFs render identically after reopening.
            var centerOffset = md.Version == "V5" ? 0.0 : labelSize / 2.0;

            var restoredPersons = md.Persons.Select(p => new Person(p.Label.Number, p.Name.Text,
                new PointF(p.Label.CenterX + (float)centerOffset, p.Label.CenterY + (float)centerOffset))
            {
                Row = p.Row
            });
            Persons.AddRange(restoredPersons);

            LabelDiameter = labelSize;

            // Upgrade older versions to V5 and store as current metadata
            if (md is AutoNumMetaData_V5 v5)
            {
                CurrentMetadata = v5;
            }
            else
            {
                // Upgrade to V5 - copy all V1/V2/V3/V4 properties into a new V5 instance
                CurrentMetadata = new AutoNumMetaData_V5
                {
                    Created = md.Created,
                    Creator = md.Creator,
                    OriginalImage = md.OriginalImage,
                    AutoNumImage = md.AutoNumImage,
                    LabelsFont = md.LabelsFont,
                    LabelsSize = md.LabelsSize,
                    NamesFont = md.NamesFont,
                    NamesEnabled = md.NamesEnabled,
                    NamesColumnCount = md.NamesColumnCount,
                    NamesRowDividersEnabled = md.NamesRowDividersEnabled,
                    NamesRowDividerTemplate = md.NamesRowDividerTemplate,
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
                    RowCount = (md is AutoNumMetaData_V4 v4) ? v4.RowCount : 1,
                    RowBoundaries = (md is AutoNumMetaData_V4 v4b) ? v4b.RowBoundaries : [],
                    // Preserve V2 properties (original image dimensions)
                    OriginalImageWidth = (md is AutoNumMetaData_V2 v2) ? v2.OriginalImageWidth : 0,
                    OriginalImageHeight = (md is AutoNumMetaData_V2 v2x) ? v2x.OriginalImageHeight : 0,
                    TitleHeight = (md is AutoNumMetaData_V2 v2y) ? v2y.TitleHeight : 0
                };
            }

            ApplyRowsFromMetadataBoundariesIfAvailable();

            // Calculate boundaries from detected rows if they don't exist yet
            // This handles V1-V3 images and edge cases where rows are assigned but boundaries aren't
            if (CurrentMetadata.RowBoundaries.Count == 0 && Persons.Count > 0)
            {
                CalculateBoundariesFromDetectedRows();
            }

            // Don't automatically show row boundaries - they'll be restored when user opens the dialog
            // The metadata is preserved in CurrentMetadata for later use
            ClearRowDefinition();

            Trace.WriteLine($"InitFromMetadata: sending MetadataLoadedMessage version={md.Version}, persons={md.Persons.Count}");
            WeakReferenceMessenger.Default.Send(new MetadataLoadedMessage(md));
            Trace.WriteLine("InitFromMetadata: MetadataLoadedMessage completed");
        }

        public void BeginRowDefinition(int rowCount, IEnumerable<RowBoundary>? boundaries = null)
        {
            var session = new RowDefinitionSession();
            if (boundaries is not null)
            {
                session.Restore(rowCount, ImageWidth, ImageHeight, boundaries);
            }
            else
            {
                session.Initialize(rowCount, ImageWidth, ImageHeight);
            }
            RowDefinitionSession = session;
        }

        public void ClearRowDefinition()
        {
            RowDefinitionSession = null;
        }

        public void ResetRowState()
        {
            ClearRowDefinition();

            if (CurrentMetadata is null)
            {
                return;
            }

            CurrentMetadata.RowCount = 1;
            CurrentMetadata.RowBoundaries = [];
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

        private void ApplyRowsFromMetadataBoundariesIfAvailable()
        {
            if (CurrentMetadata is null || CurrentMetadata.RowBoundaries.Count == 0 || Persons.Count == 0)
            {
                return;
            }

            foreach (var person in Persons)
            {
                var anchor = person.GetRowAnchorPoint();
                var row = 1;
                foreach (var boundary in CurrentMetadata.RowBoundaries)
                {
                    if (anchor.Y > boundary.GetYAtX(anchor.X, ImageWidth))
                    {
                        row++;
                    }
                }
                person.Row = row;
            }
        }

        /// <summary>
        /// Calculate and store row boundaries from the current person row assignments.
        /// Called after loading metadata where persons may have rows but no boundaries.
        /// This handles V1-V3 images and edge cases where row assignments exist but boundaries don't.
        /// </summary>
        public void CalculateBoundariesFromDetectedRows()
        {
            if (CurrentMetadata is null || Persons.Count == 0)
            {
                return;
            }

            // First check if persons have any row assignments
            var hasAnyRows = Persons.Any(p => p.Row > 0);

            if (!hasAnyRows)
            {
                // Edge case: persons with no rows - assign all to row 1
                foreach (var person in Persons)
                {
                    person.Row = 1;
                }
            }

            // Group persons by row
            var groups = Persons
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
                CurrentMetadata.RowBoundaries = [];
                CurrentMetadata.RowCount = 1;
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
                boundaryY = Math.Clamp(boundaryY, minAllowed, ImageHeight);

                boundaries.Add(new RowBoundary(boundaryY, boundaryY));
            }

            CurrentMetadata.RowBoundaries = boundaries;
            CurrentMetadata.RowCount = groups.Count;
        }

        public void UpdateMetadataBeforeSave(LabelManager lm, NameManager nm, TitleManager tm, ImageInfoManager iim, ImageIdManager idm)
        {
            if (CurrentMetadata == null)
            {
                // Create fresh metadata if none exists (e.g., new image)
                CurrentMetadata = new AutoNumMetaData_V5
                {
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
            CurrentMetadata.NamesRowDividersEnabled = nm.ShowRowDividers;
            CurrentMetadata.NamesRowDividerTemplate = nm.RowDividerTextTemplate;
            CurrentMetadata.ImageId = idm.ImageId;
            CurrentMetadata.ImageIdFont = new AutoNumFont(idm.FontColor, idm.BackgroundColor, idm.FontFamily.Name, idm.FontSize);
            CurrentMetadata.ImageIdEnabled = idm.IsEnabled;
            CurrentMetadata.TitleFont = new AutoNumFont(tm.TitleFontColor, tm.BackgroundColor, tm.TitleFontFamily.Name, tm.TitleFontSize);
            CurrentMetadata.TitleEnabled = tm.IsEnabled;
            CurrentMetadata.ImageInfoFont = new AutoNumFont(iim.ImageInfoFontColor, iim.BackgroundColor, iim.ImageInfoFontFamily.Name, iim.ImageInfoFontSize);
            CurrentMetadata.ImageInfoEnabled = iim.IsEnabled;

            SyncReconstructionMetadata(tm, iim);

            if (CurrentMetadata is AutoNumMetaData_V3 v3)
            {
                SyncSizingMetadata(v3, lm, tm, iim, idm);
            }

            // Update persons
            CurrentMetadata.Persons.Clear();
            foreach (var person in Persons)
            {
                CurrentMetadata.Persons.Add(new AutoNumPerson(person));
            }

            // Sync row boundaries from session if active
            SyncRowDefinitionToMetadata();
        }

        private void SyncReconstructionMetadata(TitleManager tm, ImageInfoManager iim)
        {
            if (CurrentMetadata is not AutoNumMetaData_V2 v2)
            {
                return;
            }

            v2.OriginalImageWidth = Bitmap?.Width ?? 0;
            v2.OriginalImageHeight = Bitmap?.Height ?? 0;

            bool hasTopText = (tm.IsEnabled && !string.IsNullOrEmpty(tm.Title))
                || (iim.IsEnabled && !string.IsNullOrEmpty(iim.ImageInfo));
            v2.TitleHeight = hasTopText ? Math.Max(0, (int)Math.Round(TitleRegionHeight)) : 0;

            Trace.WriteLine($"UpdateMetadataBeforeSave: synced reconstruction fields width={v2.OriginalImageWidth}, height={v2.OriginalImageHeight}, titleHeight={v2.TitleHeight}, hasTopText={hasTopText}");
        }

        private static void SyncSizingMetadata(AutoNumMetaData_V3 v3, LabelManager lm, TitleManager tm, ImageInfoManager iim, ImageIdManager idm)
        {
            var baseLabelDiameter = double.IsFinite(lm.BaseLabelDiameter) && lm.BaseLabelDiameter > 0
                ? lm.BaseLabelDiameter
                : MarkerLabel.Style.Diameter;
            var baseLabelFontSize = double.IsFinite(lm.BaseLabelFontSize) && lm.BaseLabelFontSize > 0
                ? lm.BaseLabelFontSize
                : MarkerLabel.Style.FontSize;
            // Namensliste/Title/Description/Image-ID resolve against BaseTextFontSize (not
            // BaseLabelFontSize) at runtime — their scales below must be relative to the same
            // base, or re-applying them on reopen would silently produce the wrong font size.
            var baseTextFontSize = double.IsFinite(lm.BaseTextFontSize) && lm.BaseTextFontSize > 0
                ? lm.BaseTextFontSize
                : baseLabelFontSize;

            v3.BaseLabelDiameter = baseLabelDiameter;
            v3.BaseLabelFontSize = baseLabelFontSize;
            v3.BaseTextFontSize = baseTextFontSize;
            v3.LabelScale = SizingModel.SafeScale(MarkerLabel.Style.Diameter, baseLabelDiameter);
            v3.NameScale = SizingModel.SafeScale(TextLabel.Style.FontSize, baseTextFontSize);
            v3.ImageIdScale = SizingModel.SafeScale(idm.FontSize, baseTextFontSize);
            v3.TitleScale = SizingModel.SafeScale(tm.TitleFontSize, baseTextFontSize);
            v3.ImageInfoScale = SizingModel.SafeScale(iim.ImageInfoFontSize, baseTextFontSize);
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
            CurrentMetadata = new AutoNumMetaData_V5
            {
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
        private AutoNumMetaData_V5? _currentMetadata;
    }
}
