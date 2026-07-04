using System.Linq;
using System.Windows.Input;

namespace AutoNumber.ViewModels
{
    public class RowDefinitionManager : BaseViewModel
    {
        public RowDefinitionManager(ImageVM imageVM, LabelManager labelManager)
        {
            _imageVM = imageVM;
            _labelManager = labelManager;

            PreviewCommand = new RelayCommand(_ => Preview(), _ => CanPreview);
            ApplyCommand = new RelayCommand(_ => Apply(), _ => CanApply);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => CanCancel);
            DeleteRowCommand = new RelayCommand(DeleteRowFromParameter, _ => CanApply);

            _imageVM.PropertyChanged += ImageVM_PropertyChanged;

            // If there's already a session with boundaries, sync the row count to it
            if (_imageVM.RowDefinitionSession != null && _imageVM.RowDefinitionSession.Boundaries.Count > 0)
            {
                _rowCount = _imageVM.RowDefinitionSession.RowCount;
            }
        }

        public int RowCount
        {
            get => _rowCount;
            set
            {
                var clamped = Math.Clamp(value, 1, 50);
                if (_rowCount != clamped)
                {
                    _rowCount = clamped;
                    OnPropertyChanged(nameof(RowCount));
                    OnPropertyChanged(nameof(RowCountText));
                    CommandManager.InvalidateRequerySuggested();

                    if (HasPreview && CanPreview)
                    {
                        _imageVM.BeginRowDefinition(RowCount);
                        ApplyRowPreviewColoring();
                        _imageVM.ApplyRowDefinition();
                        _labelManager.Numerate();
                    }
                }
            }
        }

        public string RowCountText
        {
            get => RowCount.ToString();
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                if (int.TryParse(value, out var parsed))
                {
                    RowCount = parsed;
                }
            }
        }

        public bool HasPreview => _imageVM.RowDefinitionSession is not null;
        public bool CanPreview => _imageVM.ImageWidth > 0 && _imageVM.ImageHeight > 0;
        public bool CanApply => HasPreview;
        public bool CanCancel => HasPreview;

        public bool IsRowDefinitionMode
        {
            get => _isRowDefinitionMode;
            set
            {
                if (_isRowDefinitionMode == value)
                {
                    return;
                }

                _isRowDefinitionMode = value;
                OnPropertyChanged(nameof(IsRowDefinitionMode));

                if (_suppressModeTransition)
                {
                    return;
                }

                if (_isRowDefinitionMode)
                {
                    Preview();
                    SyncModeStateFromSession();
                    return;
                }

                if (HasPreview)
                {
                    Apply();
                    CloseDialog();
                }
            }
        }

        public RelayCommand PreviewCommand { get; }
        public RelayCommand ApplyCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand DeleteRowCommand { get; }

        public void Preview()
        {
            if (!CanPreview)
            {
                SyncModeStateFromSession();
                return;
            }

            // Try to restore from metadata first
            _imageVM.RestoreRowDefinitionFromMetadata();

            // If no metadata state, seed from detected rows on fresh images.
            if (_imageVM.RowDefinitionSession == null)
            {
                if (!TryInitializeSessionFromDetectedRows())
                {
                    _rowCount = ResolveDetectedRowCount();
                    OnPropertyChanged(nameof(RowCount));
                    OnPropertyChanged(nameof(RowCountText));
                    _imageVM.BeginRowDefinition(RowCount);
                }
            }

            if (_imageVM.RowDefinitionSession is not null)
            {
                _rowCount = _imageVM.RowDefinitionSession.RowCount;
                OnPropertyChanged(nameof(RowCount));
                OnPropertyChanged(nameof(RowCountText));
            }

            // Apply preview coloring to all persons based on current row boundaries
            ApplyRowPreviewColoring();

            OnPropertyChanged(nameof(HasPreview));
            SyncModeStateFromSession();
            CommandManager.InvalidateRequerySuggested();
        }

        private void ApplyRowPreviewColoring()
        {
            if (_imageVM?.RowDefinitionSession is null || _imageVM?.Persons is null)
            {
                return;
            }

            foreach (var person in _imageVM.Persons)
            {
                var anchor = person.GetRowAnchorPoint();
                var row = ResolvePreviewRow(anchor.X, anchor.Y);
                person.RowPreviewActive = true;
                person.RowPreviewColor = GetPreviewColor(row);
            }
        }

        public bool TryInsertRowAtRightEdgeY(double rightY)
        {
            if (!HasPreview)
            {
                Preview();
            }

            if (_imageVM.RowDefinitionSession is null)
            {
                return false;
            }

            var inserted = _imageVM.RowDefinitionSession.TryInsertBoundaryAtRightY(rightY);
            if (!inserted)
            {
                return false;
            }

            _rowCount = _imageVM.RowDefinitionSession.RowCount;
            OnPropertyChanged(nameof(RowCount));
            OnPropertyChanged(nameof(RowCountText));
            CommitSessionChanges();
            CommandManager.InvalidateRequerySuggested();
            return true;
        }

        public bool TryDeleteRow(int row)
        {
            if (_imageVM.RowDefinitionSession is null)
            {
                return false;
            }

            var deleted = _imageVM.RowDefinitionSession.TryDeleteRow(row);
            if (!deleted)
            {
                return false;
            }

            _rowCount = _imageVM.RowDefinitionSession.RowCount;
            OnPropertyChanged(nameof(RowCount));
            OnPropertyChanged(nameof(RowCountText));
            CommitSessionChanges();
            CommandManager.InvalidateRequerySuggested();
            return true;
        }

        private void DeleteRowFromParameter(object? parameter)
        {
            if (parameter is int row)
            {
                TryDeleteRow(row);
                return;
            }

            if (parameter is string text && int.TryParse(text, out var parsedRow))
            {
                TryDeleteRow(parsedRow);
            }
        }

        private int ResolveDetectedRowCount()
        {
            var count = _imageVM.Persons
                .Select(person => person.Row)
                .Where(row => row > 0)
                .Distinct()
                .Count();

            return Math.Max(1, count);
        }

        private bool TryInitializeSessionFromDetectedRows()
        {
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

            if (groups.Count == 0)
            {
                return false;
            }

            if (groups.Count == 1)
            {
                _imageVM.BeginRowDefinition(1);
                return true;
            }

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

            _imageVM.BeginRowDefinitionFromBoundaries(groups.Count, boundaries);
            return true;
        }

        private int ResolvePreviewRow(double x, double y)
        {
            if (_imageVM?.RowDefinitionSession is null)
            {
                return 1;
            }

            var row = 1;
            foreach (var boundary in _imageVM.RowDefinitionSession.Boundaries)
            {
                if (y > GetBoundaryY(boundary, x, _imageVM.ImageWidth))
                {
                    row++;
                }
            }

            return row;
        }

        private static double GetBoundaryY(RowBoundary boundary, double x, double imageWidth)
        {
            var width = Math.Max(1, imageWidth);
            var t = Math.Clamp(x / width, 0.0, 1.0);
            return boundary.LeftY + (boundary.RightY - boundary.LeftY) * t;
        }

        private static System.Drawing.Color GetPreviewColor(int row)
        {
            var palette = new[]
            {
                System.Drawing.Color.FromArgb(255, 255, 82, 82),
                System.Drawing.Color.FromArgb(255, 76, 175, 80),
                System.Drawing.Color.FromArgb(255, 33, 150, 243),
                System.Drawing.Color.FromArgb(255, 255, 193, 7)
            };

            return palette[Math.Max(0, row - 1) % palette.Length];
        }


        public void Apply()
        {
            if (!CanApply)
            {
                return;
            }

            CommitSessionChanges();
            // Don't clear the session - keep the lines visible
            OnPropertyChanged(nameof(HasPreview));
            CommandManager.InvalidateRequerySuggested();
        }

        public void CloseDialog()
        {
            if (!CanCancel)
            {
                SyncModeStateFromSession();
                return;
            }

            // Sync current state to metadata before closing
            _imageVM.SyncRowDefinitionToMetadata();
            _imageVM.ClearRowDefinition();
            OnPropertyChanged(nameof(HasPreview));
            SyncModeStateFromSession();
            CommandManager.InvalidateRequerySuggested();
        }

        public void Cancel()
        {
            if (!CanCancel)
            {
                SyncModeStateFromSession();
                return;
            }

            // Discard all changes - don't save
            _imageVM.ClearRowDefinition();
            OnPropertyChanged(nameof(HasPreview));
            SyncModeStateFromSession();
            CommandManager.InvalidateRequerySuggested();
        }

        private void ImageVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ImageVM.ImageWidth) or nameof(ImageVM.ImageHeight) or nameof(ImageVM.RowDefinitionSession))
            {
                OnPropertyChanged(nameof(HasPreview));
                OnPropertyChanged(nameof(CanPreview));
                OnPropertyChanged(nameof(CanApply));
                OnPropertyChanged(nameof(CanCancel));

                if (e.PropertyName == nameof(ImageVM.RowDefinitionSession))
                {
                    SyncModeStateFromSession();
                }

                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void CommitSessionChanges()
        {
            if (_imageVM.RowDefinitionSession is null)
            {
                return;
            }

            _imageVM.SyncRowDefinitionToMetadata();
            _imageVM.ApplyRowDefinition();
            ApplyRowPreviewColoring();
            _labelManager.Numerate();
        }

        private void SyncModeStateFromSession()
        {
            var hasSession = _imageVM.RowDefinitionSession is not null;
            if (_isRowDefinitionMode == hasSession)
            {
                return;
            }

            _suppressModeTransition = true;
            _isRowDefinitionMode = hasSession;
            OnPropertyChanged(nameof(IsRowDefinitionMode));
            _suppressModeTransition = false;
        }

        private readonly ImageVM _imageVM;
        private readonly LabelManager _labelManager;
        private int _rowCount = 3;
        private bool _isRowDefinitionMode;
        private bool _suppressModeTransition;
    }
}
