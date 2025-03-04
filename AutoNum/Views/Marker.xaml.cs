using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using AutoNumber.ViewModels;

namespace AutoNumber.Views
{
    /// <summary>
    /// Interaktionslogik für Bookmark.xaml
    /// </summary>
    public partial class Marker : UserControl
    {
        public Marker(MarkerVM markerVM)
        {
            InitializeComponent();
            this.DataContext = markerVM;

            Canvas.SetLeft(this, 0);
            Canvas.SetTop(this, 0);

            if (markerVM is MarkerLabel)
                doLock(false);
            else 
                doLock(true);

            markerVM.PropertyChanged += Dc_PropertyChanged;
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(Marker), new PropertyMetadata(""));

        public double W
        {
            get { return (double)GetValue(WProperty); }
            set { SetValue(WProperty, value); }
        }
        public static readonly DependencyProperty WProperty =
            DependencyProperty.Register("W", typeof(double), typeof(Marker), new PropertyMetadata(0.0));

        public double H
        {
            get { return (double)GetValue(HProperty); }
            set { SetValue(HProperty, value); }
        }
        public static readonly DependencyProperty HProperty =
            DependencyProperty.Register("H", typeof(double), typeof(Marker), new PropertyMetadata(0.0));



        void doLock(bool locked)
        {
            if (locked)
            {
                MarkerUI.PreviewMouseDown -= imgArr_PreviewMouseDown;
                MarkerUI.PreviewMouseMove -= imgArr_PreviewMouseMove;
                MarkerUI.PreviewMouseUp -= imgArr_PreviewMouseUp;
                //MarkerUI.IsMouseDirectlyOverChanged -= MouseOverBookmarkChanged;
                //Scaler.IsMouseDirectlyOverChanged -= MouseOverBookmarkChanged;

               // bookmarkRect.StrokeThickness = new SolidColorBrush(new Color() { A = 0x60, R = 0x0, G = 0x0, B = 0xFF });
            }
            else
            {
                MarkerUI.PreviewMouseDown += imgArr_PreviewMouseDown;
                MarkerUI.PreviewMouseMove += imgArr_PreviewMouseMove;
                MarkerUI.PreviewMouseUp += imgArr_PreviewMouseUp;
                //MarkerUI.IsMouseDirectlyOverChanged += MouseOverBookmarkChanged;
                //Scaler.IsMouseDirectlyOverChanged += MouseOverBookmarkChanged;
                //bookmarkRect.StrokeThickness = new SolidColorBrush(Colors.Red);
            }
        }

        //private void MouseOverBookmarkChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    if (sender is MarkerLabel)
        //        Cursor = (bool)e.NewValue ? Cursors.SizeAll : Cursors.Arrow;
            
        //}

        private void Dc_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is MarkerVM vm)
            {
                if (e.PropertyName == "isLocked")
                {
                    doLock(vm.isLocked);
                }
            }
        }

        #region Moving --------------------------------------------------

        Point? oldMousePosition;

        private void imgArr_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            oldMousePosition = e.GetPosition(Parent as FrameworkElement);
            //Cursor = Cursors.Hand;

            MarkerUI.CaptureMouse();
            e.Handled = true;
        }

        private void imgArr_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (oldMousePosition != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var newMousePosition = e.GetPosition(Parent as UIElement);

                var deltaMousePosition = newMousePosition - oldMousePosition.Value;
                oldMousePosition = newMousePosition;

                double curX = Canvas.GetLeft(this);
                double curY = Canvas.GetTop(this);

                Canvas.SetLeft(this, curX + deltaMousePosition.X);
                Canvas.SetTop(this, curY + deltaMousePosition.Y);

                // flip(newMousePosition.X);
            }

            e.Handled = true;

        }

        //public void flip(double xPos)
        //{
        //    bool left = xPos < 1500;

        //    var st = bookmarkRect.RenderTransform as TransformGroup;
        //    var y = (ScaleTransform)st!.Children.First(tr => tr is ScaleTransform);
        //    y.ScaleX = left ? 1 : -1;
        //}

        private void imgArr_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            oldMousePosition = null;
            MarkerUI.ReleaseMouseCapture();
            e.Handled = true;
        }

        #endregion

        #region Scaling ---------------------------------------
        // Point? oldScalerPosition;
        //// private bool imgArr_SsMouseOver;

        // private void Scaler_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        // {
        //     oldScalerPosition = e.GetPosition(Parent as FrameworkElement);

        //     Scaler.CaptureMouse();
        //     e.Handled = true;
        // }

        // private void Scaler_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        // {
        //     oldScalerPosition = null;
        //     Scaler.ReleaseMouseCapture();
        //     e.Handled = true;
        // }

        // private void Scaler_PreviewMouseMove(object sender, MouseEventArgs e)
        // {
        //     if (oldScalerPosition != null && e.LeftButton == MouseButtonState.Pressed)
        //     {
        //         Point newScalerPosition = e.GetPosition(Parent as UIElement);
        //         Vector delta = newScalerPosition - oldScalerPosition.Value;
        //         oldScalerPosition = newScalerPosition;

        //         W = Math.Max(txt.ActualWidth + 60, bookmarkRect.Width + delta.X);
        //         H = Math.Max(70, bookmarkRect.Height + delta.Y);
        //     }

        //     e.Handled = true;
        // }

        #endregion



        //private void EditDetails(object sender, RoutedEventArgs e)
        //{
        //    if (markerVM is BookmarkVM dc)
        //    {
        //        var detailsView = new BookmarkDetailsView(dc);
        //        detailsView.Owner = Application.Current.MainWindow;
        //        detailsView.Show();
        //    }
        //}
    }
}

