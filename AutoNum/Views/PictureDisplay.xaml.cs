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
using System.Windows.Input;
using MahApps.Metro.IconPacks;

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

        public ImageVM Picture
        {
            get => (ImageVM)GetValue(PictureProperty);
            set => SetValue(PictureProperty, value);
        }

        public static readonly DependencyProperty PictureProperty =
            DependencyProperty.Register(nameof(Picture), typeof(ImageVM), typeof(PictureDisplay), new PropertyMetadata(null, OnPictureChanged));


        static void OnPictureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var that = (PictureDisplay)d;

            if (e.OldValue is ImageVM oldPageVM)
            {
                oldPageVM.Persons.CollectionChanged -= that.Marker_CollectionChanged;
                oldPageVM.PropertyChanged -= that.PictureVM_PropertyChanged;
                that.AttachRowDefinitionSession(null);
            }

            if (e.NewValue is not ImageVM pageVM)
            {
                return;
            }

            pageVM.Persons.CollectionChanged += that.Marker_CollectionChanged;
            pageVM.PropertyChanged += that.PictureVM_PropertyChanged;

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

                    foreach (Person person in Picture.Persons)
                    {
                        AddMarker(person.Label);
                        AddMarker(person.Name);

                        if (_rowDefinitionSession is not null)
                        {
                            person.Label.PropertyChanged += Label_PositionChanged;
                        }
                    }
                    break;
            }

            if (_rowDefinitionSession is not null)
            {
                RenderRowDefinitionOverlay();
            }
        }

        void RemoveMarker(MarkerVM markerVM)
        {
            var markerUIs = PictureCanvas.Children.OfType<Marker>();  // we are only interested in canvas-children of type Marker

            PictureCanvas.Children.Remove(markerUIs.FirstOrDefault(m => m.Uid == markerVM.Id.ToString()));

        }

        void ClearMarkers()
        {
            var ml = PictureCanvas.Children.OfType<Marker>().ToList();
            foreach (var marker in ml)
            {
                PictureCanvas.Children.Remove(marker);
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

            int idx = PictureCanvas.Children.Add(marker);
            PictureCanvas.Children[idx].Uid = markerVM.Id.ToString();
        }

        private void UpdatePersonRowAndColor(Person person)
        {
            if (_rowDefinitionSession is null || Picture is null)
            {
                return;
            }

            var anchor = person.GetRowAnchorPoint();
            var row = ResolvePreviewRow(anchor.X, anchor.Y);
            person.Row = row;
            person.RowPreviewActive = true;
            person.RowPreviewColor = RowDefinitionSession.GetPreviewColor(row);
        }

        private void PictureVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImageVM.RowDefinitionSession))
            {
                AttachRowDefinitionSession(Picture.RowDefinitionSession);
                return;
            }

            if (e.PropertyName is nameof(ImageVM.ImageWidth) or nameof(ImageVM.ImageHeight))
            {
                if (Picture.ImageWidth > 0 && Picture.ImageHeight > 0)
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

                if (Picture is not null)
                {
                    _rowDefinitionSession.ClearPreview(Picture.Persons);
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
            if (Picture is null)
            {
                return;
            }

            foreach (var person in Picture.Persons)
            {
                person.Label.PropertyChanged += Label_PositionChanged;
            }
        }

        private void UnsubscribeAllLabelsFromPositionChanges()
        {
            if (Picture is null)
            {
                return;
            }

            foreach (var person in Picture.Persons)
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
            if (_rowDefinitionSession is null || Picture is null || Picture.ImageWidth <= 0 || Picture.ImageHeight <= 0)
            {
                rowDefinitionOverlay.Children.Clear();
                rowDefinitionOverlay.Visibility = Visibility.Collapsed;
                _rowBoundaryVisuals.Clear();
                rowChipHost.Children.Clear();
                rowInsertGhost.Visibility = Visibility.Collapsed;
                return;
            }

            UpdateRowEditStripLayout();

            rowDefinitionOverlay.Visibility = Visibility.Visible;
            rowDefinitionOverlay.Children.Clear();
            _rowBoundaryVisuals.Clear();

            var width = Picture.ImageWidth;
            var boundaries = _rowDefinitionSession.Boundaries.ToList();

            foreach (var boundary in boundaries)
            {
                CreateBoundaryVisual(boundary.LeftY, boundary.RightY, width);
            }

            RenderRowStripChips();
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
            CommitRowAssignmentsFromSession();
            RenderRowStripChips();
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
            UpdateBoundaryVisual(state, state.LeftY, state.RightY, Picture?.ImageWidth ?? 0);
            ApplyPreviewFromVisuals();
        }

        private void ClampBoundaryVisual(RowBoundaryVisualState state)
        {
            if (Picture is null)
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
            var maxLeft = index == _rowBoundaryVisuals.Count - 1 ? Picture.ImageHeight : _rowBoundaryVisuals[index + 1].LeftY - minimumGap;
            var minRight = index == 0 ? 0.0 : _rowBoundaryVisuals[index - 1].RightY + minimumGap;
            var maxRight = index == _rowBoundaryVisuals.Count - 1 ? Picture.ImageHeight : _rowBoundaryVisuals[index + 1].RightY - minimumGap;

            state.LeftY = Math.Clamp(state.LeftY, minLeft, maxLeft);
            state.RightY = Math.Clamp(state.RightY, minRight, maxRight);
        }

        private void SyncVisualsToSession()
        {
            if (_rowDefinitionSession is null || Picture is null)
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

        private void CommitRowAssignmentsFromSession()
        {
            if (_rowDefinitionSession is null || Picture is null || DataContext is not MainVM mainVM)
            {
                return;
            }

            _rowDefinitionSession.ApplyToPersons(Picture.Persons);
            mainVM.LabelManager.Numerate();
        }

        private void ApplyPreviewFromVisuals()
        {
            if (Picture is null)
            {
                return;
            }

            foreach (var person in Picture.Persons)
            {
                var anchor = person.GetRowAnchorPoint();
                var row = ResolvePreviewRow(anchor.X, anchor.Y);
                person.RowPreviewActive = true;
                person.RowPreviewColor = RowDefinitionSession.GetPreviewColor(row);
            }
        }

        private int ResolvePreviewRow(double x, double y) =>
            RowBoundaryMath.ResolveRow(x, y,
                _rowBoundaryVisuals.Select(state => (state.LeftY, state.RightY)),
                Picture?.ImageWidth ?? 0);

        private double GetBoundaryHandleSize()
        {
            var baseSize = (DataContext as MainVM)?.LabelManager.BaseLabelDiameter
                           ?? Picture?.LabelDiameter
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
            if (border is null || PictureCanvas is null)
            {
                return;
            }

            if (TryGetContentBounds(requireImage: true, out var bounds))
            {
                border.ZoomToFit(bounds);
            }
        }

        public void ZoomToImage()
        {
            if (border is null || PictureCanvas is null || Picture is null)
            {
                return;
            }

            if (!photoImage.IsVisible || photoImage.ActualWidth <= 0 || photoImage.ActualHeight <= 0)
            {
                return;
            }

            // Get image bounds (always needed as the base)
            var imageBounds = photoImage.TransformToAncestor(PictureCanvas).TransformBounds(new Rect(0, 0, photoImage.ActualWidth, photoImage.ActualHeight));

            // If in row mode and row edit strip is visible, include it in zoom
            if (rowEditStrip.Visibility == Visibility.Visible)
            {
                // The row strip is positioned to the right of the image
                // Get its bounds and union with image bounds
                var stripBounds = rowEditStrip.TransformToAncestor(PictureCanvas).TransformBounds(new Rect(0, 0, rowEditStrip.ActualWidth, rowEditStrip.ActualHeight));
                var combinedBounds = Rect.Union(imageBounds, stripBounds);
                border.ZoomToFit(combinedBounds);
            }
            else
            {
                // Just zoom to image when row mode is off
                border.ZoomToFit(imageBounds);
            }
        }

        private bool TryGetContentBounds(bool requireImage, out Rect bounds)
        {
            bounds = Rect.Empty;

            if (requireImage && (!photoImage.IsVisible || photoImage.ActualWidth <= 0 || photoImage.ActualHeight <= 0))
            {
                return false;
            }

            Rect? contentBounds = null;
            foreach (var element in new FrameworkElement[] { photoImage, topTextPanel, imageIdBorder, namesRegionBorder })
            {
                if (!element.IsVisible || element.ActualWidth <= 0 || element.ActualHeight <= 0)
                {
                    continue;
                }

                var rect = element.TransformToAncestor(PictureCanvas).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                contentBounds = contentBounds is null ? rect : Rect.Union(contentBounds.Value, rect);
            }

            if (contentBounds is not Rect computed || computed.Width <= 0 || computed.Height <= 0)
            {
                return false;
            }

            bounds = computed;
            return true;
        }

        private void UpdateRowEditStripLayout()
        {
            if (Picture is null)
            {
                return;
            }

            var handleSize = Math.Max(8, GetBoundaryHandleSize());

            // Dimensions in units of handle width
            _rowStripHandleDistance = handleSize * 0.2;
            _rowStripChipWidth = handleSize * 1.5;
            _rowStripDeleteButtonSize = 1.5* handleSize;
            _rowStripTotalWidth = _rowStripChipWidth + _rowStripHandleDistance + _rowStripDeleteButtonSize;

            Canvas.SetLeft(rowEditStrip, Picture.ImageWidth + handleSize / 2.0 + _rowStripHandleDistance);
            rowEditStrip.Width = _rowStripTotalWidth;

            rowInsertGhost.X1 = Math.Max(3, _rowStripChipWidth * 0.08);
            rowInsertGhost.X2 = Math.Max(rowInsertGhost.X1 + 4, _rowStripChipWidth - _rowStripChipWidth * 0.08);
            rowInsertGhost.Stroke = new SolidColorBrush(Color.FromArgb(230, 0, 188, 212));
            rowInsertGhost.StrokeThickness = handleSize * 0.2;
            rowInsertGhost.StrokeDashArray = null;
            Panel.SetZIndex(rowInsertGhost, 90);

            var headerSize = Math.Clamp(handleSize * 0.9, 10, 22);
            rowStripHeader.Width = headerSize * 2.5;
            rowStripHeader.Height = headerSize * 2.5;
            Canvas.SetLeft(rowStripHeader, Math.Max(0, (_rowStripChipWidth - rowStripHeader.Width) / 2.0));
            Canvas.SetTop(rowStripHeader, -Math.Max(10, rowStripHeader.Height * 1.5));

            Canvas.SetLeft(rowChipHost, 0);
            rowChipHost.Width = _rowStripChipWidth;
        }

        private void RowEditStrip_MouseMove(object sender, MouseEventArgs e)
        {
            if (_rowDefinitionSession is null || Picture is null || Picture.ImageHeight <= 0)
            {
                rowInsertGhost.Visibility = Visibility.Collapsed;
                return;
            }

            var position = e.GetPosition(rowEditStrip);
            if (position.X < 0 || position.X > _rowStripChipWidth)
            {
                rowInsertGhost.Visibility = Visibility.Collapsed;
                return;
            }

            var y = Math.Clamp(position.Y, 0, Picture.ImageHeight);
            rowInsertGhost.Y1 = y;
            rowInsertGhost.Y2 = y;
            rowInsertGhost.Visibility = Visibility.Visible;
        }

        private void RowEditStrip_MouseLeave(object sender, MouseEventArgs e)
        {
            rowInsertGhost.Visibility = Visibility.Collapsed;
        }

        private void RowEditStrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MainVM mainVM || !mainVM.RowDefinitionManager.IsRowDefinitionMode)
            {
                return;
            }

            if (_rowDefinitionSession is null || Picture is null)
            {
                return;
            }

            if (FindParent<Button>(e.OriginalSource as DependencyObject) is not null)
            {
                return;
            }

            var taggedElement = FindParent<FrameworkElement>(e.OriginalSource as DependencyObject);
            if (taggedElement?.Tag is int)
            {
                return;
            }

            var position = e.GetPosition(rowEditStrip);
            if (position.X < 0 || position.X > _rowStripChipWidth)
            {
                return;
            }

            var y = Math.Clamp(position.Y, 0, Picture.ImageHeight);
            if (mainVM.RowDefinitionManager.TryInsertRowAtRightEdgeY(y))
            {
                RenderRowDefinitionOverlay();
            }

            e.Handled = true;
        }

        private void RenderRowStripChips()
        {
            rowChipHost.Children.Clear();

            if (_rowDefinitionSession is null || Picture is null || Picture.ImageHeight <= 0)
            {
                return;
            }

            var rows = _rowDefinitionSession.GetRowsAtRightEdge();
            foreach (var row in rows)
            {
                var region = CreateRowRegion(row.Row, row.Top, row.Bottom);
                rowChipHost.Children.Add(region);
            }

            foreach (var row in rows)
            {
                var centerY = (row.Top + row.Bottom) / 2.0;
                var chip = CreateRowChip(row.Row, centerY, row.Bottom - row.Top);
                rowChipHost.Children.Add(chip);
            }
        }

        private FrameworkElement CreateRowRegion(int row, double top, double bottom)
        {
            var clampedTop = Math.Clamp(top, 0, Picture?.ImageHeight ?? top);
            var clampedBottom = Math.Clamp(bottom, clampedTop, Picture?.ImageHeight ?? bottom);
            var regionColor = RowDefinitionSession.GetPreviewColor(row);
            var borderThickness = Math.Max(1, _rowStripChipWidth * 0.05);
            var topBorderThickness = clampedTop <= 0.5 ? borderThickness : 0;

            var region = new Border
            {
                Width = _rowStripChipWidth,
                Height = Math.Max(1, clampedBottom - clampedTop),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(regionColor.A, regionColor.R, regionColor.G, regionColor.B)),
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(borderThickness, topBorderThickness, borderThickness, borderThickness),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(region, 0);
            Canvas.SetTop(region, clampedTop);
            return region;
        }

        private FrameworkElement CreateRowChip(int row, double centerY, double availableHeight)
        {
            var buttonSize = Math.Min(Math.Max(8, _rowStripDeleteButtonSize), Math.Max(8, availableHeight));
            var iconSize = Math.Max(8, buttonSize * 0.6);

            var deleteButton = new Button
            {
                Content = new PackIconMaterial
                {
                    Kind = PackIconMaterialKind.DeleteForeverOutline,
                    Width = iconSize,
                    Height = iconSize,
                    Foreground = Brushes.Black,
                    Background = Brushes.White
                },
                BorderThickness = new Thickness(6),
                Width = buttonSize,
                Height = buttonSize,
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Style = TryFindResource("MahApps.Styles.Button.Circle") as Style,
                Tag = row,
                ToolTip = $"Reihe R{row} entfernen"
            };
            deleteButton.Click += RowChipDelete_Click;

            var canDelete = _rowDefinitionSession is not null && _rowDefinitionSession.RowCount > 1;
            deleteButton.IsEnabled = canDelete;
            deleteButton.Opacity = canDelete ? 0.9 : 0.3;

            var buttonLeft = _rowStripChipWidth + (_rowStripHandleDistance * 0.5);
            Canvas.SetLeft(deleteButton, Math.Clamp(buttonLeft, 0, Math.Max(0, _rowStripTotalWidth - deleteButton.Width)));
            Canvas.SetTop(deleteButton, Math.Clamp(centerY - deleteButton.Height / 2.0, 0, Math.Max(0, Picture!.ImageHeight - deleteButton.Height)));
            return deleteButton;
        }

        private void RowChipDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button deleteButton || deleteButton.Tag is not int row)
            {
                return;
            }

            if (DataContext is MainVM mainVM && mainVM.RowDefinitionManager.DeleteRowCommand.CanExecute(row))
            {
                mainVM.RowDefinitionManager.DeleteRowCommand.Execute(row);
                RenderRowDefinitionOverlay();
            }

            e.Handled = true;
        }

        private static T? FindParent<T>(DependencyObject? element) where T : class
        {
            var current = element;
            while (current is not null)
            {
                if (current is T candidate)
                {
                    return candidate;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
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
        private double _rowStripHandleDistance = 12;
        private double _rowStripChipWidth = 36;
        private double _rowStripDeleteButtonSize = 24;
        private double _rowStripTotalWidth = 72;
    }
}
