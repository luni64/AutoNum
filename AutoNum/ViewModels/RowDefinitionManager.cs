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

        public RelayCommand PreviewCommand { get; }
        public RelayCommand ApplyCommand { get; }
        public RelayCommand CancelCommand { get; }

        public void Preview()
        {
            if (!CanPreview)
            {
                return;
            }

            // Try to restore from metadata first
            _imageVM.RestoreRowDefinitionFromMetadata();

            // If no metadata state, create a new session
            if (_imageVM.RowDefinitionSession == null)
            {
                _imageVM.BeginRowDefinition(RowCount);
            }
            else if (_imageVM.RowDefinitionSession.Boundaries.Count > 0)
            {
                // Update row count to match restored session
                _rowCount = _imageVM.RowDefinitionSession.RowCount;
                OnPropertyChanged(nameof(RowCount));
                OnPropertyChanged(nameof(RowCountText));
            }

            // Apply preview coloring to all persons based on current row boundaries
            ApplyRowPreviewColoring();

            OnPropertyChanged(nameof(HasPreview));
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
                System.Drawing.Color.FromArgb(255, 224, 242, 254),
                System.Drawing.Color.FromArgb(255, 255, 244, 214),
                System.Drawing.Color.FromArgb(255, 243, 229, 245),
                System.Drawing.Color.FromArgb(255, 232, 245, 233)
            };

            return palette[Math.Max(0, row - 1) % palette.Length];
        }


        public void Apply()
        {
            if (!CanApply)
            {
                return;
            }

            _imageVM.SyncRowDefinitionToMetadata();
            _imageVM.ApplyRowDefinition();
            _labelManager.Numerate();
            // Don't clear the session - keep the lines visible
            OnPropertyChanged(nameof(HasPreview));
            CommandManager.InvalidateRequerySuggested();
        }

        public void CloseDialog()
        {
            if (!CanCancel)
            {
                return;
            }

            // Sync current state to metadata before closing
            _imageVM.SyncRowDefinitionToMetadata();
            _imageVM.ClearRowDefinition();
            OnPropertyChanged(nameof(HasPreview));
            CommandManager.InvalidateRequerySuggested();
        }

        public void Cancel()
        {
            if (!CanCancel)
            {
                return;
            }

            // Discard all changes - don't save
            _imageVM.ClearRowDefinition();
            OnPropertyChanged(nameof(HasPreview));
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
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private readonly ImageVM _imageVM;
        private readonly LabelManager _labelManager;
        private int _rowCount = 3;
    }
}
