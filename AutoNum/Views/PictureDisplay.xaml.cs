using AutoNumber.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

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
            var that = (PictureDisplay)d;

            if (e.OldValue is ImageVM oldPageVM)
            {
                oldPageVM.Persons.CollectionChanged -= that.Marker_CollectionChanged;
                oldPageVM.PropertyChanged -= that.PageVM_PropertyChanged;
            }

            if (e.NewValue is not ImageVM pageVM)
            {
                return;
            }

            pageVM.Persons.CollectionChanged += that.Marker_CollectionChanged;
            pageVM.PropertyChanged += that.PageVM_PropertyChanged;

            that.ClearMarkers();
            foreach (var person in pageVM.Persons)
            {
                that.AddMarker(person.Label);
                that.AddMarker(person.Name);
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

        private void PageVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ImageVM.ImageWidth) or nameof(ImageVM.ImageHeight))
            {
                if (Page.ImageWidth > 0 && Page.ImageHeight > 0)
                {
                    _pendingInitialZoomToFit = true;
                    _pendingFitAttempts = 0;
                    Dispatcher.BeginInvoke(TryApplyPendingZoomToFit, DispatcherPriority.ContextIdle);
                }

                return;
            }

            if (_pendingInitialZoomToFit && e.PropertyName is nameof(ImageVM.NamesRegionHeight) or nameof(ImageVM.TitleRegionHeight))
            {
                Dispatcher.BeginInvoke(TryApplyPendingZoomToFit, DispatcherPriority.ContextIdle);
            }
        }

        private void TryApplyPendingZoomToFit()
        {
            if (!_pendingInitialZoomToFit)
            {
                return;
            }

            if (!TryGetContentBounds(requireImage: true, out var bounds))
            {
                if (_pendingFitAttempts++ < 8)
                {
                    Dispatcher.BeginInvoke(TryApplyPendingZoomToFit, DispatcherPriority.ContextIdle);
                }

                return;
            }

            border.ZoomToFit(bounds);
            _pendingInitialZoomToFit = false;
        }

        public void ZoomToFit()
        {
            if (border is null || PageCanvas is null)
            {
                return;
            }

            if (TryGetContentBounds(requireImage: true, out var bounds))
            {
                border.ZoomToFit(bounds);
            }
        }

        private bool TryGetContentBounds(bool requireImage, out Rect bounds)
        {
            bounds = Rect.Empty;

            if (requireImage && (!pageimg.IsVisible || pageimg.ActualWidth <= 0 || pageimg.ActualHeight <= 0))
            {
                return false;
            }

            Rect? contentBounds = null;
            foreach (var element in new FrameworkElement[] { pageimg, topTextPanel, imageIdBorder, namesRegionBorder })
            {
                if (!element.IsVisible || element.ActualWidth <= 0 || element.ActualHeight <= 0)
                {
                    continue;
                }

                var rect = element.TransformToAncestor(PageCanvas).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                contentBounds = contentBounds is null ? rect : Rect.Union(contentBounds.Value, rect);
            }

            if (contentBounds is not Rect computed || computed.Width <= 0 || computed.Height <= 0)
            {
                return false;
            }

            bounds = computed;
            return true;
        }

        private void TopTextPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainVM mainVM)
            {
                mainVM.PictureVM.TitleRegionHeight = e.NewSize.Height;
            }
        }

        private bool _pendingInitialZoomToFit;
        private int _pendingFitAttempts;
    }
}
