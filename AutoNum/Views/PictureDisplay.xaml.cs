using AutoNumber.ViewModels;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AutoNumber.Views
{
    /// <summary>
    /// Interaction logic for PictureDisplay.xaml
    /// </summary>
    public partial class PictureDisplay : UserControl
    {
        public PictureDisplay()
        {
            InitializeComponent();
        }

        public ImageVM Page
        {
            get => (ImageVM)GetValue(PageProperty);
            set => SetValue(PageProperty, value);
        }

        public static readonly DependencyProperty PageProperty =
            DependencyProperty.Register("Page", typeof(ImageVM), typeof(PictureDisplay), new PropertyMetadata(null, OnPageChanged));


        static void OnPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ImageVM pageVM)
            {
                var that = ((PictureDisplay)d);
                pageVM.Persons.CollectionChanged -= that.Marker_CollectionChanged; // remove old handler
                pageVM.Persons.CollectionChanged += that.Marker_CollectionChanged;

                that.ClearMarkers();
                foreach (var person in pageVM.Persons)
                {
                    that.AddMarker(person.Label);
                    that.AddMarker(person.Name);
                }
            }
        }

        private void Marker_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    foreach (Person person in e.NewItems!)
                    {
                        AddMarker(person.Label);
                        AddMarker(person.Name);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Person person in e.OldItems!)
                    {
                        RemoveMarker(person.Label);
                        RemoveMarker(person.Name);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ClearMarkers();
                    break;
            }
        }

        void RemoveMarker(MarkerVM markerVM)
        {
            var markerUIs = PageCanvas.Children.OfType<Marker>();  // we are only interested in canvas-children of type Marker

            PageCanvas.Children.Remove(markerUIs.FirstOrDefault(m => m.Uid == markerVM.Id.ToString()));

        }

        void ClearMarkers()
        {
            var ml = PageCanvas.Children.OfType<Marker>().ToList();
            foreach (var marker in ml)
            {
                PageCanvas.Children.Remove(marker);
            }
        }

        void AddMarker(MarkerVM markerVM)
        {            
            var marker = new Marker(markerVM);

            marker.SetBinding(Canvas.TopProperty, new Binding
            {
                Source = markerVM,
                Path = new PropertyPath("Y"),
                Mode = BindingMode.TwoWay,
            });

            marker.SetBinding(Canvas.LeftProperty, new Binding
            {
                Source = markerVM,
                Path = new PropertyPath("X"),
                Mode = BindingMode.TwoWay,
            });

            marker.SetBinding(Marker.WProperty, new Binding
            {
                Source = markerVM,
                Path = new PropertyPath("W"),
                Mode = BindingMode.TwoWay,
            });

            marker.SetBinding(Marker.HProperty, new Binding
            {
                Source = markerVM,
                Path = new PropertyPath("H"),
                Mode = BindingMode.TwoWay,
            });

            Canvas.SetLeft(marker, markerVM.X);
            Canvas.SetTop(marker, markerVM.Y);


            int idx = PageCanvas.Children.Add(marker);
            PageCanvas.Children[idx].Uid = markerVM.Id.ToString();
        }

        private void TopTextPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainVM mainVM)
            {
                mainVM.PictureVM.TitleRegionHeight = e.NewSize.Height;
            }
        }
    }
}