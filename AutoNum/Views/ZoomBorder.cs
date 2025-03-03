using NumberIt.ViewModels;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;


namespace NumberIt.Views
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

        private void ZoomBorder_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPosition = e.GetPosition(this);

            var c = this.child as Canvas;
            var t = this.TransformToVisual(c);
            var np = t.Transform(clickPosition);

            UIElement clickedElement = this.InputHitTest(clickPosition) as UIElement;

            var markerLabel = FindParentWithDataContext<MarkerLabel>(clickedElement);

            if (DataContext is MainVM mainVM)
            {
                if (markerLabel != null)
                {
                    //var person = markerLabel.person;
                    mainVM.pictureVM.Persons.Remove(markerLabel.person);
                }
                else
                {
                    int lastIdx = 0;                
                    if (mainVM.pictureVM.Persons.Any())
                    {
                        lastIdx = mainVM.pictureVM.Persons.Max(p => p.Label.Number);
                    }

                    var center = new System.Drawing.PointF((float)(np.X - MarkerLabel.Diameter / 2), (float)(np.Y - MarkerLabel.Diameter / 2));
                    mainVM.pictureVM.Persons.Add(new Person(lastIdx + 1, "", center));
                    

                    //mainVM.pictureVM.MarkerVMs.Add(
                    //    new MarkerLabel
                    //    {
                    //        CenterX = np.X - MarkerLabel.Diameter / 2,
                    //        CenterY = np.Y - MarkerLabel.Diameter / 2,
                    //        Number = (lastIdx + 1).ToString()
                    //    });
                }
            }
        }

        private T FindParentWithDataContext<T>(DependencyObject element) where T : class
        {
            while (element != null)
            {
                if (element is FrameworkElement fe && fe.DataContext is T dataContext)
                {
                    return dataContext;
                }
                element = VisualTreeHelper.GetParent(element);
            }
            return null;
        }

        public void Reset()
        {
            if (child != null)
            {
                // reset zoom
                var st = GetScaleTransform(child);
                Zoom = 1.0;
                //st.ScaleX = 1.0;
                //st.ScaleY = 1.0;

                // reset pan
                var tt = GetTranslateTransform(child);
                PanX = 0.0;
                PanY = 0.0;
                //tt.X = 0.0;
                //tt.Y = 0.0;
            }
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
                //Trace.WriteLine($"onZoom {e.NewValue}");
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
                //Trace.WriteLine($"onPnX {e.NewValue}");
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
                //Trace.WriteLine($"onPnY {e.NewValue}");
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

        //void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    this.Reset();
        //}

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

