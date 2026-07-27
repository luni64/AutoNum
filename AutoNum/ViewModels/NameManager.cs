using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Specialized;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Data;
using System.Collections.Generic;

namespace AutoNumber.ViewModels
{
    public class NameManager : BaseViewModel
    {
        public ICollectionView PersonsView { get; }

        bool _isEnabled = false;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                SetProperty(ref _isEnabled, value);
                ShowNames();
            }
        }

        /// <summary>
        /// Defers ShowNames() until the outermost scope disposes, so a handler that sets several
        /// layout-relevant properties as one logical operation (e.g. restoring saved metadata)
        /// recomputes once instead of once per property. Reentrant.
        /// </summary>
        private IDisposable BeginUpdate() => new UpdateScope(this);

        private int _updateDepth;
        private bool _showNamesPending;

        private sealed class UpdateScope : IDisposable
        {
            private readonly NameManager _owner;
            private bool _disposed;

            public UpdateScope(NameManager owner)
            {
                _owner = owner;
                _owner._updateDepth++;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                if (--_owner._updateDepth == 0 && _owner._showNamesPending)
                {
                    _owner._showNamesPending = false;
                    _owner.ShowNames();
                }
            }
        }

        public void ShowNames()
        {
            if (_updateDepth > 0)
            {
                _showNamesPending = true;
                return;
            }

            if (_imageVM.Persons.Count == 0)
            {
                _imageVM.NamesRegionHeight = 0;
                RowDividers = [];
                return;
            }

            if (IsEnabled)
            {
                var imageIdOffset = _imageIdManager.ShowImageIdLine ? _imageIdManager.LineHeight : 0;
                var placement = Analyzer.PlacePersonNames(
                    PersonsView,
                    _imageVM.ImageWidth,
                    _imageVM.ImageHeight + imageIdOffset,
                    NameTableColumnCount,
                    ShowRowDividers,
                    FormatRowDividerText);
                _imageVM.NamesRegionHeight = placement.TotalHeight;
                RowDividers = placement.Dividers;
            }
            else
            {
                foreach (Person person in PersonsView)
                {
                    person.Name.Visible = false;
                }

                _imageVM.NamesRegionHeight = 0;
                RowDividers = [];
            }
        }

        private Color _fontColor = Color.Black;
        public Color FontColor
        {
            get => _fontColor;
            set
            {
                SetProperty(ref _fontColor, value);
                TextLabel.Style.FontColor = value;
            }
        }

        private Color _backgroundColor = Color.White;
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }

        /// <summary>
        /// Font scale factor (0.25–4.0). Model property that drives actual font size.
        /// This is what the UI should bind to, with slider-position conversion happening in XAML.
        /// </summary>
        double _fontScale = 1.0;
        public double FontScale
        {
            get => _fontScale;
            set
            {
                var clamped = Math.Clamp(value, 0.25, 4.0);
                if (_fontScale != clamped)
                {
                    _fontScale = clamped;
                    ApplyScale();
                    ShowNames();
                    OnPropertyChanged();
                }
            }
        }

        public int NameTableColumnCount
        {
            get => _nameTableColumnCount;
            set
            {
                var clamped = Math.Clamp(value, 1, 4);
                if (_nameTableColumnCount != clamped)
                {
                    _nameTableColumnCount = clamped;
                    ShowNames();
                    OnPropertyChanged();
                }
            }
        }

        public FontFamily FontFamily
        {
            get => _fontFamily;
            set
            {
                if (_fontFamily == value)
                {
                    return;
                }

                SetProperty(ref _fontFamily, value);
                TextLabel.Style.FontFamily = value;
            }
        }

        public bool ShowRowDividers
        {
            get => _showRowDividers;
            set
            {
                if (_showRowDividers != value)
                {
                    SetProperty(ref _showRowDividers, value);
                    ShowNames();
                }
            }
        }

        public string RowDividerTextTemplate
        {
            get => _rowDividerTextTemplate;
            set
            {
                var normalized = string.IsNullOrWhiteSpace(value) ? DefaultRowDividerTextTemplate : value.Trim();
                if (!string.Equals(_rowDividerTextTemplate, normalized, StringComparison.Ordinal))
                {
                    SetProperty(ref _rowDividerTextTemplate, normalized);
                    ShowNames();
                }
            }
        }

        public IReadOnlyList<NameListDividerRenderItem> RowDividers
        {
            get => _rowDividers;
            private set => SetProperty(ref _rowDividers, value);
        }

        public void Refresh()
        {
            if (PersonsView is IEditableCollectionView ecv)
            {
                if (!ecv.IsEditingItem && !ecv.IsAddingNew)
                {
                    PersonsView.Refresh();
                }
            }
        }

        /// <summary>
        /// Refresh() can itself trigger a ShowNames() (via PersonsView's Reset notification), so
        /// calling Refresh() followed by ShowNames() separately recomputes twice. Batches them into one.
        /// </summary>
        public void RefreshAndShowNames()
        {
            using var _ = BeginUpdate();
            Refresh();
            ShowNames();
        }

        public NameManager(ImageVM imageVM, LabelManager labelManager, ImageIdManager imageIdManager)
        {
            _imageVM = imageVM;
            _labelManager = labelManager;
            _imageIdManager = imageIdManager;

            PersonsView = CollectionViewSource.GetDefaultView(imageVM.Persons);
            PersonsView.SortDescriptions.Add(new SortDescription("Label.Number", ListSortDirection.Ascending));
            PersonsView.CollectionChanged += PersonsView_CollectionChanged;

            FontScale = 1.0; // Initialize to default scale

            WeakReferenceMessenger.Default.Register<LabelsChangedMessage>(this, (r, msg) =>
            {
                using var _ = BeginUpdate();
                Refresh();
                ApplyScale();
                ShowNames();
            });

            WeakReferenceMessenger.Default.Register<MetadataLoadedMessage>(this, (r, msg) =>
            {
                using var _ = BeginUpdate();
                try
                {
                    var md = msg.Metadata;
                    Trace.WriteLine($"MetadataLoaded[NameManager]: start version={md.Version}, namesEnabled={md.NamesEnabled}");

                    BackgroundColor = Color.FromArgb(md.NamesFont.Background);
                    FontColor = Color.FromArgb(md.NamesFont.Foreground);
                    FontFamily = FontFamilyResolver.Resolve(md.NamesFont.Family, FontFamily);

                    var scale = md is AutoNumMetaData_V3 v3
                        ? v3.NameScale
                        : ResolveLegacyScale(md.NamesFont.Size, md.LabelsFont.Size);

                    FontScale = scale;
                    NameTableColumnCount = Math.Clamp(md.NamesColumnCount ?? 1, 1, 4);
                    ShowRowDividers = md.NamesRowDividersEnabled ?? true;
                    RowDividerTextTemplate = string.IsNullOrWhiteSpace(md.NamesRowDividerTemplate)
                        ? DefaultRowDividerTextTemplate
                        : md.NamesRowDividerTemplate;
                    IsEnabled = _imageVM.Persons.Count > 0 && (md.NamesEnabled ?? true);
                    ShowNames();

                    Trace.WriteLine($"MetadataLoaded[NameManager]: scale={scale:F4}, storedSize={md.NamesFont.Size:F4}, baseLabelStored={md.LabelsFont.Size:F4}, baseLabelRuntime={_labelManager.BaseLabelFontSize:F4}, resolvedFontSize={TextLabel.Style.FontSize:F4}, columns={NameTableColumnCount}, enabled={IsEnabled}");
                    Trace.WriteLine("MetadataLoaded[NameManager]: completed");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"MetadataLoaded[NameManager]: failed - {ex}");
                    throw;
                }
            });

            _imageIdManager.PropertyChanged += ImageIdManager_PropertyChanged;
        }

        private void PersonsView_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)  // attach handlers for property changes
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Person person in e.NewItems!)
                    {
                        person.PropertyChanged += Person_PropertyChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Person person in e.OldItems!)
                    {
                        person.PropertyChanged -= Person_PropertyChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    // Reset carries no OldItems/NewItems (e.g. Clear(), or BulkObservableCollection.AddRange),
                    // so re-sync subscriptions against whatever is currently in the collection.
                    foreach (Person person in _imageVM.Persons)
                    {
                        person.PropertyChanged -= Person_PropertyChanged;
                        person.PropertyChanged += Person_PropertyChanged;
                    }
                    break;

            }
            ShowNames();
        }

        private void Person_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Only re-sort/re-layout the names table for properties that actually affect it:
            // row membership and FullName (raised for number and name-text changes). Purely
            // visual per-marker state — IsSelected (raised on every hover move) and the
            // RowPreview* pair (set on ALL persons on every boundary-drag delta) — used to
            // trigger a full relayout each time, making hover and row dragging O(n²).
            // Empty/null PropertyName is the batched "assume everything changed" convention.
            if (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName is nameof(Person.Row) or nameof(Person.FullName))
            {
                Refresh();
                ShowNames();
            }
        }

        private void ImageIdManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Empty/null PropertyName is the "several properties changed at once, assume everything
            // did" convention raised by SuspendNotifications() after a batched update.
            if (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName is nameof(ImageIdManager.LineHeight)
                or nameof(ImageIdManager.ShowImageIdLine)
                or nameof(ImageIdManager.IsEnabled)
                or nameof(ImageIdManager.ImageId))
            {
                ShowNames();
            }
        }

        private void ApplyScale()
        {
            var baseTextFontSize = _labelManager.BaseTextFontSize;
            if (baseTextFontSize <= 0)
            {
                return;
            }

            TextLabel.Style.FontSize = SizingModel.ResolveSize(baseTextFontSize, FontScale);
        }

        private static double ResolveLegacyScale(double actualNameFontSize, double legacyLabelFontSize)
        {
            return SizingModel.SafeScale(actualNameFontSize, legacyLabelFontSize);
        }

        public string FormatRowDividerText(int row)
        {
            var template = string.IsNullOrWhiteSpace(RowDividerTextTemplate)
                ? DefaultRowDividerTextTemplate
                : RowDividerTextTemplate;

            var displayRow = ResolveDisplayRowNumber(row);

            if (template.Contains("{n}", StringComparison.OrdinalIgnoreCase))
            {
                return template.Replace("{n}", displayRow.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            var tokenIndex = template.LastIndexOf('n');
            if (tokenIndex >= 0)
            {
                return template[..tokenIndex] + displayRow + template[(tokenIndex + 1)..];
            }

            return template;
        }

        /// <summary>
        /// Row number printed in a divider. With bottom-up numbering the front (bottom) row holds
        /// number 1, so labelling it "Reihe 3" would contradict the list — the stored index is
        /// mirrored for display only. Person.Row and the row-edit mode keep counting from the top,
        /// so the underlying data and the boundary editor are untouched.
        /// </summary>
        private int ResolveDisplayRowNumber(int row)
        {
            if (!_labelManager.NumberBottomUp || row <= 0)
            {
                return row;
            }

            var maxRow = _imageVM.Persons
                .Where(person => person.Row > 0)
                .Select(person => person.Row)
                .DefaultIfEmpty(0)
                .Max();

            return maxRow > 0 ? maxRow - row + 1 : row;
        }

        private const string DefaultRowDividerTextTemplate = "Reihe n:";

        private readonly ImageVM _imageVM;
        private readonly LabelManager _labelManager;
        private readonly ImageIdManager _imageIdManager;
        private FontFamily _fontFamily = new("Calibri");
        private int _nameTableColumnCount = 1;
        private bool _showRowDividers = true;
        private string _rowDividerTextTemplate = DefaultRowDividerTextTemplate;
        private IReadOnlyList<NameListDividerRenderItem> _rowDividers = [];
    }
}

