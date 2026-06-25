using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Specialized;
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
            if (_imageVM.Persons.Count == 0) return;

            if (IsEnabled)
            {
                var height = Analyzer.PlacePersonNames(PersonsView, _imageVM.ImageWidth, _imageVM.ImageHeight);
                _imageVM.NamesRegionHeight = height;
            }
            else
            {
                foreach (Person person in PersonsView)
                {
                    person.Name.Visible = false;
                }
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

        double _fontSizeSliderValue;
        public double FontSizeSliderValue
        {
            get => _fontSizeSliderValue;
            set
            {
                if (_fontSizeSliderValue != value)
                {
                    _fontSizeSliderValue = value;
                    TextLabel.Style.FontSize = DefaultFontSize * (0.5 + 0.0002 * (_fontSizeSliderValue * _fontSizeSliderValue));
                    ShowNames();
                    OnPropertyChanged();
                }
            }
        }
        public static double DefaultFontSize = 80;

        public FontFamily FontFamily { get; set; } = new FontFamily("Calibri");

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

        public NameManager(ImageVM imageVM)
        {
            _imageVM = imageVM;

            PersonsView = CollectionViewSource.GetDefaultView(imageVM.Persons);
            PersonsView.SortDescriptions.Add(new SortDescription("Label.Number", ListSortDirection.Ascending));
            PersonsView.CollectionChanged += PersonsView_CollectionChanged;

            FontSizeSliderValue = 50;

            WeakReferenceMessenger.Default.Register<LabelsChangedMessage>(this, (r, msg) =>
            {
                Refresh();
                ShowNames();
            });

            WeakReferenceMessenger.Default.Register<NewImageOpenedMessage>(this, (r, msg) =>
            {
                DefaultFontSize = 80;
            });

            WeakReferenceMessenger.Default.Register<MetadataLoadedMessage>(this, (r, msg) =>
            {
                var md = msg.Metadata;
                DefaultFontSize = 80;
                BackgroundColor = Color.FromArgb(md.NamesFont.Background);
                FontColor = Color.FromArgb(md.NamesFont.Foreground);
                FontFamily = new FontFamily(md.NamesFont.Family);

                FontSizeSliderValue = (double.IsFinite(md.NamesFont.Size) && md.NamesFont.Size > 0)
                    ? ToSliderValue(md.NamesFont.Size, DefaultFontSize)
                    : 50;

                IsEnabled = _imageVM.Persons.Count > 0 && (md.NamesEnabled ?? true);
                ShowNames();
            });
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

        private static double ToSliderValue(double fontSize, double defaultFontSize)
        {
            if (!double.IsFinite(fontSize) || !double.IsFinite(defaultFontSize) || defaultFontSize <= 0)
            {
                return 50;
            }

            var normalized = (fontSize / defaultFontSize) - 0.5;
            if (normalized <= 0)
            {
                return 0;
            }

            return Math.Clamp(Math.Sqrt(normalized / 0.0002), 0, 100);
        }

        private readonly ImageVM _imageVM;
    }
}

