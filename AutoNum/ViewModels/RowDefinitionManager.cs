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

            // Try to restore from saved state first
            _imageVM.RestoreRowDefinitionFromSavedState();

            // If no saved state, create a new session
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

            OnPropertyChanged(nameof(HasPreview));
            CommandManager.InvalidateRequerySuggested();
        }

        public void Apply()
        {
            if (!CanApply)
            {
                return;
            }

            _imageVM.SaveRowDefinitionToMetadata();
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

            // Save the current state before closing so it can be restored next time dialog opens
            _imageVM.SaveRowDefinitionToMetadata();
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
