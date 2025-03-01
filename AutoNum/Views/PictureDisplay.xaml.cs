using NumberIt.ViewModels;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Converters;




namespace NumberIt.Views
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

        public ImageModel Page
        {
            get => (ImageModel)GetValue(PageProperty);
            set => SetValue(PageProperty, value);
        }

        public static readonly DependencyProperty PageProperty =
            DependencyProperty.Register("Page", typeof(ImageModel), typeof(PictureDisplay), new PropertyMetadata(null, OnPageChanged));


        static void OnPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ImageModel pageVM)
            {
                var that = ((PictureDisplay)d);
                pageVM.MarkerVMs.CollectionChanged -= that.Marker_CollectionChanged;
                pageVM.MarkerVMs.CollectionChanged += that.Marker_CollectionChanged;

                that.ClearMarkers();
                foreach (var markerVM in pageVM.MarkerVMs)
                {
                    that.AddMarker(markerVM);
                }
            }
        }

        private void Marker_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    foreach (MarkerVM bm in e.NewItems!)
                    {
                        AddMarker(bm);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveMarker(e.OldItems!);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ClearMarkers();
                    break;
            }

        }

        void RemoveMarker(IList MarkerVMs)
        {
            var markerUIs = PageCanvas.Children.OfType<Marker>();  // we are only interested in canvas-children of type Bookmark

            foreach (MarkerVM markerVM in MarkerVMs)
            {
                var markerUI = markerUIs.FirstOrDefault(b => b.Uid == markerVM.Id.ToString());  // find the canvas child which corresponds to the removed view marriageModel
                if (markerUI != null)
                {
                    PageCanvas.Children.Remove(markerUI);
                }
            }
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
            if (markerVM is MarkerLabel label &&  string.IsNullOrEmpty(label.Number))
            {
               label.Number =  "?";
            }

            var marker = new Marker(markerVM);

            //marker.SetBinding(MarkerUI.TextProperty, new Binding
            //{
            //    Source = markerVM,
            //    Path = new PropertyPath("Title"),
            //    Mode = BindingMode.TwoWay,
            //    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            //});

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


            //marker.Width = markerVM.W;
            //marker.Height = markerVM.H;

            

            Canvas.SetLeft(marker, markerVM.X);
            Canvas.SetTop(marker, markerVM.Y);
            // marker.flip(markerVM.X);


            int idx = PageCanvas.Children.Add(marker);
            PageCanvas.Children[idx].Uid = markerVM.Id.ToString();
        }

        //public override string ToString()
        //{
        //    var dc = DataContext as BookVM;
        //    return $"B.{dc!.SelectedPage?.SheetNr} Book:{dc.Title}";
        //}

        private void AddBookmarkClick(object sender, RoutedEventArgs e)
        {
            //if (DataContext is BookVM dc)
            //{
            //    var pos = Mouse.GetPosition(pageimg);                
            //    dc.cmdAddBookmark.Execute(((int)pos.X,(int)pos.Y));
            //}
        }

        private void tbTitle_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(DataContext is MainVM mainVM)
            {
                mainVM.pictureVM.TitleRegionHeight = e.NewSize.Height;
                //mainVM.TitleVM.TitleHeight = e.NewSize.Height;
                var x = tbTitle.Padding;
            }

        }
    }

}
;