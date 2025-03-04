using Emgu.CV.ML;
using AutoNumber.Model;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Data;

namespace AutoNumber.ViewModels
{
    public class NameManager : BaseViewModel
    {
        public ICollectionView PersonsView { get; }

        public MainVM parent { get; set; }

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
            var pvm = parent.pictureVM;
            if (IsEnabled)
            {
                var height = Analyzer.PlacePersonNames(PersonsView, pvm.ImageWidth, pvm.ImageHeight);
                pvm.NamesRegionHeight = height;
            }
            else
            {
                foreach (Person person in PersonsView)
                {
                    person.Name.visible = false;
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
                var pvm = parent.pictureVM;
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

        public FontFamily FontFamily { get; } = new FontFamily("Calibri");

        public void refresh()
        {
            if (PersonsView is IEditableCollectionView ecv)
            {
                if (!ecv.IsEditingItem && !ecv.IsAddingNew)
                {
                    PersonsView.Refresh();
                }
            }
        }

        public NameManager(MainVM parent)
        {
            this.parent = parent;

            PersonsView = CollectionViewSource.GetDefaultView(parent.pictureVM.Persons);
            PersonsView.SortDescriptions.Add(new SortDescription("Label.Number", ListSortDirection.Ascending));
            PersonsView.CollectionChanged += PersonsView_CollectionChanged;

            FontSizeSliderValue = 50;
        }

        private void PersonsView_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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
            refresh();
            ShowNames();
        }
    }
}

