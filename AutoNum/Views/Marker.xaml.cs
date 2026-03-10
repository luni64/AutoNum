using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            }
            else
            {
                MarkerUI.PreviewMouseDown += imgArr_PreviewMouseDown;
                MarkerUI.PreviewMouseMove += imgArr_PreviewMouseMove;
                MarkerUI.PreviewMouseUp += imgArr_PreviewMouseUp;
            }
        }

        private void Dc_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is MarkerVM vm)
            {
                if (e.PropertyName == "IsLocked")
                {
                    doLock(vm.IsLocked);
                }
            }
        }

        #region Moving --------------------------------------------------

        Point? oldMousePosition;

        private void imgArr_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            oldMousePosition = e.GetPosition(Parent as FrameworkElement);

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
            }

            e.Handled = true;

        }

        private void imgArr_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            oldMousePosition = null;
            MarkerUI.ReleaseMouseCapture();
            e.Handled = true;
        }

        #endregion
    }
}
