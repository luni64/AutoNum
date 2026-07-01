using AutoNumber.ViewModels;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
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
                that.AttachRowDefinitionSession(null);
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

            that.AttachRowDefinitionSession(pageVM.RowDefinitionSession);
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

                        // If row definition session is active, subscribe new label to position changes
                        if (_rowDefinitionSession is not null)
                        {
                            person.Label.PropertyChanged += Label_PositionChanged;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Person person in e.OldItems!)
                    {
                        RemoveMarker(person.Label);
                        RemoveMarker(person.Name);

                        // Unsubscribe from position changes
                        person.Label.PropertyChanged -= Label_PositionChanged;
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ClearMarkers();
                    UnsubscribeAllLabelsFromPositionChanges();
                    break;
            }

            if (_rowDefinitionSession is not null)
            {
                RenderRowDefinitionOverlay();
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

        private void UpdatePersonRowAndColor(Person person)
        {
            if (_rowDefinitionSession is null || Page is null)
            {
                return;
            }

            var anchor = person.GetRowAnchorPoint();
            var row = ResolvePreviewRow(anchor.X, anchor.Y);
            person.Row = row;
            person.RowPreviewActive = true;
            person.RowPreviewColor = GetPreviewColor(row);
        }

        private void PageVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImageVM.RowDefinitionSession))
            {
                AttachRowDefinitionSession(Page.RowDefinitionSession);
                return;
            }

            if (e.PropertyName is nameof(ImageVM.ImageWidth) or nameof(ImageVM.ImageHeight))
            {
                if (Page.ImageWidth > 0 && Page.ImageHeight > 0)
                {
                    _pendingInitialZoomToFit = true;
                    _pendingFitAttempts = 0;
                    Dispatcher.BeginInvoke(TryApplyPendingZoomToFit, DispatcherPriority.ContextIdle);
                }

                if (_rowDefinitionSession is not null)
                {
                    RenderRowDefinitionOverlay();
                }

                return;
            }

            if (_pendingInitialZoomToFit && e.PropertyName is nameof(ImageVM.NamesRegionHeight) or nameof(ImageVM.TitleRegionHeight))
            {
                Dispatcher.BeginInvoke(TryApplyPendingZoomToFit, DispatcherPriority.ContextIdle);
            }
        }

        private void AttachRowDefinitionSession(RowDefinitionSession? session)
        {
            if (_rowDefinitionSession is not null)
            {
                _rowDefinitionSession.PropertyChanged -= RowDefinitionSession_PropertyChanged;
                _rowDefinitionSession.Boundaries.CollectionChanged -= RowDefinitionBoundaries_CollectionChanged;

                if (Page is not null)
                {
                    _rowDefinitionSession.ClearPreview(Page.Persons);
                    UnsubscribeAllLabelsFromPositionChanges();
                }

                foreach (var boundary in _rowDefinitionSession.Boundaries)
                {
                    UnsubscribeBoundary(boundary);
                }
            }

            _rowDefinitionSession = session;

            if (_rowDefinitionSession is null)
            {
                rowDefinitionOverlay.Children.Clear();
                rowDefinitionOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            _rowDefinitionSession.PropertyChanged += RowDefinitionSession_PropertyChanged;
            _rowDefinitionSession.Boundaries.CollectionChanged += RowDefinitionBoundaries_CollectionChanged;

            foreach (var boundary in _rowDefinitionSession.Boundaries)
            {
                SubscribeBoundary(boundary);
            }

            // Subscribe all existing labels to position changes so they update row assignments while dragging
            SubscribeAllLabelsToPositionChanges();

            RenderRowDefinitionOverlay();
        }

        private void SubscribeAllLabelsToPositionChanges()
        {
            if (Page is null)
            {
                return;
            }

            foreach (var person in Page.Persons)
            {
                person.Label.PropertyChanged += Label_PositionChanged;
            }
        }

        private void UnsubscribeAllLabelsFromPositionChanges()
        {
            if (Page is null)
            {
                return;
            }

            foreach (var person in Page.Persons)
            {
                person.Label.PropertyChanged -= Label_PositionChanged;
            }
        }

        private void Label_PositionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is MarkerLabel markerLabel && 
                (e.PropertyName == nameof(MarkerLabel.X) || e.PropertyName == nameof(MarkerLabel.Y) || 
                 e.PropertyName == nameof(MarkerLabel.CenterX) || e.PropertyName == nameof(MarkerLabel.CenterY)))
            {
                UpdatePersonRowAndColor(markerLabel.Person);
            }
        }

        private void RowDefinitionSession_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RenderRowDefinitionOverlay();
        }

        private void RowDefinitionBoundaries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (RowBoundary boundary in e.OldItems)
                {
                    UnsubscribeBoundary(boundary);
                }
            }

            if (e.NewItems is not null)
            {
                foreach (RowBoundary boundary in e.NewItems)
                {
                    SubscribeBoundary(boundary);
                }
            }

            RenderRowDefinitionOverlay();
        }

        private void SubscribeBoundary(RowBoundary boundary)
        {
            PropertyChangedEventHandler handler = RowBoundary_PropertyChanged;
            _rowBoundaryHandlers[boundary] = handler;
            boundary.PropertyChanged += handler;
        }

        private void UnsubscribeBoundary(RowBoundary boundary)
        {
            if (_rowBoundaryHandlers.TryGetValue(boundary, out var handler))
            {
                boundary.PropertyChanged -= handler;
                _rowBoundaryHandlers.Remove(boundary);
            }
        }

        private void RowBoundary_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RenderRowDefinitionOverlay();
        }

        private void RenderRowDefinitionOverlay()
        {
            if (_rowDefinitionSession is null || Page is null || Page.ImageWidth <= 0 || Page.ImageHeight <= 0)
            {
                rowDefinitionOverlay.Children.Clear();
                rowDefinitionOverlay.Visibility = Visibility.Collapsed;
                _rowBoundaryVisuals.Clear();
                return;
            }

            rowDefinitionOverlay.Visibility = Visibility.Visible;
            rowDefinitionOverlay.Children.Clear();
            _rowBoundaryVisuals.Clear();

            var width = Page.ImageWidth;
            var boundaries = _rowDefinitionSession.Boundaries.ToList();

            foreach (var boundary in boundaries)
            {
                CreateBoundaryVisual(boundary.LeftY, boundary.RightY, width);
            }
        }

        private RowBoundaryVisualState CreateBoundaryVisual(double leftY, double rightY, double width)
        {
            var handleSize = GetBoundaryHandleSize();
            var state = new RowBoundaryVisualState
            {
                ShadowLine = new Line
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    StrokeThickness = 6,
                    IsHitTestVisible = false
                },
                MainLine = new Line
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(230, 0, 188, 212)),
                    StrokeThickness = 2.5,
                    IsHitTestVisible = false
                },
                MoveThumb = new Thumb
                {
                    Style = (Style)FindResource("RowBoundaryLineThumbStyle"),
                    Width = width,
                    Height = 32,
                    Tag = new RowBoundaryDragInfo(_rowBoundaryVisuals.Count, RowBoundaryDragTarget.Line)
                },
                LeftThumb = new Thumb
                {
                    Style = (Style)FindResource("RowBoundaryThumbStyle"),
                    Width = handleSize,
                    Height = handleSize,
                    Tag = new RowBoundaryDragInfo(_rowBoundaryVisuals.Count, RowBoundaryDragTarget.LeftAnchor)
                },
                RightThumb = new Thumb
                {
                    Style = (Style)FindResource("RowBoundaryThumbStyle"),
                    Width = handleSize,
                    Height = handleSize,
                    Tag = new RowBoundaryDragInfo(_rowBoundaryVisuals.Count, RowBoundaryDragTarget.RightAnchor)
                },
                HandleSize = handleSize
            };

            state.MoveThumb.DragDelta += BoundaryThumb_DragDelta;
            state.LeftThumb.DragDelta += BoundaryThumb_DragDelta;
            state.RightThumb.DragDelta += BoundaryThumb_DragDelta;
            state.MoveThumb.DragCompleted += BoundaryThumb_DragCompleted;
            state.LeftThumb.DragCompleted += BoundaryThumb_DragCompleted;
            state.RightThumb.DragCompleted += BoundaryThumb_DragCompleted;

            UpdateBoundaryVisual(state, leftY, rightY, width);
            Canvas.SetLeft(state.LeftThumb, -state.HandleSize / 2);
            Canvas.SetLeft(state.RightThumb, width - state.HandleSize / 2);

            Panel.SetZIndex(state.ShadowLine, 10);
            Panel.SetZIndex(state.MainLine, 11);
            Panel.SetZIndex(state.MoveThumb, 20);
            Panel.SetZIndex(state.LeftThumb, 21);
            Panel.SetZIndex(state.RightThumb, 21);

            _rowBoundaryVisuals.Add(state);
            rowDefinitionOverlay.Children.Add(state.ShadowLine);
            rowDefinitionOverlay.Children.Add(state.MainLine);
            rowDefinitionOverlay.Children.Add(state.MoveThumb);
            rowDefinitionOverlay.Children.Add(state.LeftThumb);
            rowDefinitionOverlay.Children.Add(state.RightThumb);
            return state;
        }

        private void BoundaryThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not RowBoundaryDragInfo dragInfo)
            {
                return;
            }

            if (dragInfo.Index < 0 || dragInfo.Index >= _rowBoundaryVisuals.Count)
            {
                return;
            }

            var state = _rowBoundaryVisuals[dragInfo.Index];
            MoveBoundaryVisual(state, dragInfo.Target, e.VerticalChange);
        }

        private void BoundaryThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            SyncVisualsToSession();
        }

        private void MoveBoundaryVisual(RowBoundaryVisualState state, RowBoundaryDragTarget target, double deltaY)
        {
            switch (target)
            {
                case RowBoundaryDragTarget.Line:
                    state.LeftY += deltaY;
                    state.RightY += deltaY;
                    break;
                case RowBoundaryDragTarget.LeftAnchor:
                    state.LeftY += deltaY;
                    break;
                case RowBoundaryDragTarget.RightAnchor:
                    state.RightY += deltaY;
                    break;
            }

            ClampBoundaryVisual(state);
            UpdateBoundaryVisual(state, state.LeftY, state.RightY, Page?.ImageWidth ?? 0);
            ApplyPreviewFromVisuals();
        }

        private void ClampBoundaryVisual(RowBoundaryVisualState state)
        {
            if (Page is null)
            {
                return;
            }

            var index = _rowBoundaryVisuals.IndexOf(state);
            if (index < 0)
            {
                return;
            }

            const double minimumGap = 8.0;
            var minLeft = index == 0 ? 0.0 : _rowBoundaryVisuals[index - 1].LeftY + minimumGap;
            var maxLeft = index == _rowBoundaryVisuals.Count - 1 ? Page.ImageHeight : _rowBoundaryVisuals[index + 1].LeftY - minimumGap;
            var minRight = index == 0 ? 0.0 : _rowBoundaryVisuals[index - 1].RightY + minimumGap;
            var maxRight = index == _rowBoundaryVisuals.Count - 1 ? Page.ImageHeight : _rowBoundaryVisuals[index + 1].RightY - minimumGap;

            state.LeftY = Math.Clamp(state.LeftY, minLeft, maxLeft);
            state.RightY = Math.Clamp(state.RightY, minRight, maxRight);
        }

        private void SyncVisualsToSession()
        {
            if (_rowDefinitionSession is null || Page is null)
            {
                return;
            }

            for (var index = 0; index < _rowBoundaryVisuals.Count && index < _rowDefinitionSession.Boundaries.Count; index++)
            {
                var state = _rowBoundaryVisuals[index];
                var boundary = _rowDefinitionSession.Boundaries[index];
                boundary.LeftY = state.LeftY;
                boundary.RightY = state.RightY;
            }
        }

        private void ApplyPreviewFromVisuals()
        {
            if (Page is null)
            {
                return;
            }

            foreach (var person in Page.Persons)
            {
                var anchor = person.GetRowAnchorPoint();
                var row = ResolvePreviewRow(anchor.X, anchor.Y);
                person.RowPreviewActive = true;
                person.RowPreviewColor = GetPreviewColor(row);
            }
        }

        private int ResolvePreviewRow(double x, double y)
        {
            var row = 1;
            foreach (var boundary in _rowBoundaryVisuals)
            {
                if (y > GetBoundaryY(boundary, x))
                {
                    row++;
                }
            }

            return row;
        }

        private static double GetBoundaryY(RowBoundaryVisualState state, double x)
        {
            var width = Math.Max(1, state.MainLine.X2);
            var t = Math.Clamp(x / width, 0.0, 1.0);
            return state.LeftY + (state.RightY - state.LeftY) * t;
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

        private double GetBoundaryHandleSize()
        {
            var baseSize = (DataContext as MainVM)?.LabelManager.BaseLabelDiameter
                           ?? Page?.LabelDiameter
                           ?? 0;
            return baseSize * 0.5;
        }

        private static void UpdateBoundaryVisual(RowBoundaryVisualState state, double leftY, double rightY, double width)
        {
            state.LeftY = leftY;
            state.RightY = rightY;

            state.ShadowLine.X1 = 0;
            state.ShadowLine.Y1 = leftY;
            state.ShadowLine.X2 = width;
            state.ShadowLine.Y2 = rightY;

            state.MainLine.X1 = 0;
            state.MainLine.Y1 = leftY;
            state.MainLine.X2 = width;
            state.MainLine.Y2 = rightY;

            state.MoveThumb.Width = width;
            state.MoveThumb.Height = Math.Max(state.HandleSize * 2, Math.Abs(rightY - leftY) + state.HandleSize);
            Canvas.SetLeft(state.MoveThumb, 0);
            Canvas.SetTop(state.MoveThumb, Math.Min(leftY, rightY) - state.HandleSize / 2);

            Canvas.SetTop(state.LeftThumb, leftY - state.HandleSize / 2);
            Canvas.SetTop(state.RightThumb, rightY - state.HandleSize / 2);
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

        private sealed record RowBoundaryDragInfo(int Index, RowBoundaryDragTarget Target);
        private enum RowBoundaryDragTarget
        {
            Line,
            LeftAnchor,
            RightAnchor
        }

        private sealed class RowBoundaryVisualState
        {
            public required Line ShadowLine { get; init; }
            public required Line MainLine { get; init; }
            public required Thumb MoveThumb { get; init; }
            public required Thumb LeftThumb { get; init; }
            public required Thumb RightThumb { get; init; }
            public required double HandleSize { get; init; }
            public double LeftY { get; set; }
            public double RightY { get; set; }
        }

        private bool _pendingInitialZoomToFit;
        private int _pendingFitAttempts;
        private RowDefinitionSession? _rowDefinitionSession;
        private readonly Dictionary<RowBoundary, PropertyChangedEventHandler> _rowBoundaryHandlers = [];
        private readonly List<RowBoundaryVisualState> _rowBoundaryVisuals = [];
    }
}
