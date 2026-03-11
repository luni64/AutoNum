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
            if (_imageModel.Persons.Count == 0) return;

            if (IsEnabled)
            {
                var height = Analyzer.PlacePersonNames(PersonsView, _imageModel.ImageWidth, _imageModel.ImageHeight);
                _imageModel.NamesRegionHeight = height;
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
                TextLabel.FontColor = value;
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
                    TextLabel.FontSize = DefaultFontSize * (0.5 + 0.0002 * (_fontSizeSliderValue * _fontSizeSliderValue));
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

        public NameManager(ImageModel imageModel)
        {
            _imageModel = imageModel;

            PersonsView = CollectionViewSource.GetDefaultView(imageModel.Persons);
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
                if (_imageModel.Persons.Count > 0) IsEnabled = true;
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

        private readonly ImageModel _imageModel;
    }
}

