using AutoNumber.Infrastructure;
using AutoNumber.Model;
using AutoNumber.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AutoNumber.Views
{
    public class ZoomBorder : Border
    {
        private UIElement? child = null;
        private Point origin;
        private Point start;


        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);

        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {
            this.child = element;
            if (child != null)
            {
                TransformGroup group = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                group.Children.Add(st);
                TranslateTransform tt = new TranslateTransform();
                group.Children.Add(tt);
                child.RenderTransform = group;
                child.RenderTransformOrigin = new Point(0.0, 0.0);
                this.MouseWheel += child_MouseWheel;
                this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                this.MouseMove += child_MouseMove;
                this.PreviewMouseRightButtonDown += ZoomBorder_PreviewMouseRightButtonDown;
            }
        }

        // Right-click on a label deletes its person, right-click on an empty spot adds one.
        // Hit-testing/coordinate transformation is view work; the actual add/remove logic
        // (row resolution, numbering) lives in LabelManager.
        private void ZoomBorder_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MainVM mainVM)
            {
                return;
            }

            Point clickPosition = e.GetPosition(this);
            var clickedElement = (UIElement)this.InputHitTest(clickPosition);
            var markerLabel = VisualTreeHelpers.FindAncestorDataContext<MarkerLabel>(clickedElement);

            if (markerLabel is not null)
            {
                mainVM.LabelManager.RemovePerson(markerLabel.Person);
            }
            else
            {
                var canvasPosition = this.TransformToVisual(this.child as Canvas).Transform(clickPosition);
                mainVM.LabelManager.AddPersonAt(new System.Drawing.PointF((float)canvasPosition.X, (float)canvasPosition.Y));
            }
        }

        public void Reset()
        {
            if (child != null)
            {
                // reset zoom
                var st = GetScaleTransform(child);
                Zoom = 1.0;

                // reset pan
                var tt = GetTranslateTransform(child);
                PanX = 0.0;
                PanY = 0.0;
            }
        }

        public void ZoomToFit(Rect contentBounds)
        {
            if (child is null)
            {
                return;
            }

            if (ActualWidth <= 0 || ActualHeight <= 0 || contentBounds.Width <= 0 || contentBounds.Height <= 0)
            {
                return;
            }

            var fitZoom = 0.95 * Math.Min(ActualWidth / contentBounds.Width, ActualHeight / contentBounds.Height);
            if (!double.IsFinite(fitZoom) || fitZoom <= 0)
            {
                return;
            }

            Zoom = fitZoom;
            PanX = (ActualWidth - contentBounds.Width * fitZoom) / 2 - contentBounds.X * fitZoom;
            PanY = (ActualHeight - contentBounds.Height * fitZoom) / 2 - contentBounds.Y * fitZoom;
        }

        #region DependencyProperties ------------------------------




        public double Zoom
        {
            get { return (double)GetValue(ZoomFactorProperty); }
            set { SetValue(ZoomFactorProperty, value); }
        }
        public static readonly DependencyProperty ZoomFactorProperty = DependencyProperty.Register("Zoom", typeof(double), typeof(ZoomBorder), new PropertyMetadata(1.0, OnZoomChanged));
        static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var zb = d as ZoomBorder;
            if (zb!.child != null)
            {
                var st = zb.GetScaleTransform(zb.child);
                st.ScaleX = (double)e.NewValue;
                st.ScaleY = st.ScaleX;
            }
        }

        public double PanX
        {
            get { return (double)GetValue(PanXProperty); }
            set { SetValue(PanXProperty, value); }
        }
        public static readonly DependencyProperty PanXProperty = DependencyProperty.Register("PanX", typeof(double), typeof(ZoomBorder), new PropertyMetadata(0.0, OnPanXChanged));
        static void OnPanXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var zb = d as ZoomBorder;
            if (zb != null)
            {
                var tx = zb.GetTranslateTransform(zb.child!);
                tx.X = (double)e.NewValue;
            }
        }

        public double PanY
        {
            get { return (double)GetValue(PanYProperty); }
            set { SetValue(PanYProperty, value); }
        }
        public static readonly DependencyProperty PanYProperty = DependencyProperty.Register("PanY", typeof(double), typeof(ZoomBorder), new PropertyMetadata(0.0, OnPanYChanged));
        static void OnPanYChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var zb = d as ZoomBorder;
            if (zb != null)
            {
                var tx = zb.GetTranslateTransform(zb.child!);
                tx.Y = (double)e.NewValue;
            }
        }

        #endregion

        #region Child Events

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);

                double zoom = e.Delta > 0 ? .1 : -.1;
                if (!(e.Delta > 0) && (st.ScaleX < .1 || st.ScaleY < .1))
                    return;

                Point relative = e.GetPosition(child);
                double absoluteX;
                double absoluteY;

                absoluteX = relative.X * st.ScaleX + tt.X;
                absoluteY = relative.Y * st.ScaleY + tt.Y;

                double zoomCorrected = zoom * st.ScaleX;
                Zoom = st.ScaleX += zoomCorrected;
                PanX = absoluteX - relative.X * st.ScaleX;
                PanY = absoluteY - relative.Y * st.ScaleY;
            }
        }

        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                var tt = GetTranslateTransform(child);
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                this.Cursor = Cursors.Hand;
                child.CaptureMouse();
            }
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                child.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
            }
        }

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (child != null)
            {
                if (child.IsMouseCaptured)
                {
                    var tt = GetTranslateTransform(child);
                    Vector v = start - e.GetPosition(this);
                    PanX = origin.X - v.X;
                    PanY = origin.Y - v.Y;
                }
            }
        }

        #endregion
    }


}

