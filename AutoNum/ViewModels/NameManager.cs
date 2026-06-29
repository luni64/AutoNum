using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Specialized;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Data;

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

        public void ShowNames()
        {
            if (_imageVM.Persons.Count == 0)
            {
                _imageVM.NamesRegionHeight = 0;
                return;
            }

            if (IsEnabled)
            {
                var imageIdOffset = _imageIdManager.ShowImageIdLine ? _imageIdManager.LineHeight : 0;
                var height = Analyzer.PlacePersonNames(PersonsView, _imageVM.ImageWidth, _imageVM.ImageHeight + imageIdOffset);
                _imageVM.NamesRegionHeight = height;
            }
            else
            {
                foreach (Person person in PersonsView)
                {
                    person.Name.Visible = false;
                }

                _imageVM.NamesRegionHeight = 0;
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
                Refresh();
                ApplyScale();
                ShowNames();
            });

            WeakReferenceMessenger.Default.Register<MetadataLoadedMessage>(this, (r, msg) =>
            {
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
                    IsEnabled = _imageVM.Persons.Count > 0 && (md.NamesEnabled ?? true);
                    ShowNames();

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
                    break;

            }
            ShowNames();
        }

        private void Person_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Refresh();
            ShowNames();
        }

        private void ImageIdManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ImageIdManager.LineHeight)
                or nameof(ImageIdManager.ShowImageIdLine)
                or nameof(ImageIdManager.IsEnabled)
                or nameof(ImageIdManager.ImageId))
            {
                ShowNames();
            }
        }

        private void ApplyScale()
        {
            var baseLabelFontSize = _labelManager.BaseLabelFontSize;
            if (baseLabelFontSize <= 0)
            {
                return;
            }

            TextLabel.Style.FontSize = SizingModel.ResolveSize(baseLabelFontSize, FontScale);
        }

        private static double ResolveLegacyScale(double actualNameFontSize, double legacyLabelFontSize)
        {
            return SizingModel.SafeScale(actualNameFontSize, legacyLabelFontSize);
        }

        private readonly ImageVM _imageVM;
        private readonly LabelManager _labelManager;
        private readonly ImageIdManager _imageIdManager;
        private FontFamily _fontFamily = new("Calibri");
    }
}

