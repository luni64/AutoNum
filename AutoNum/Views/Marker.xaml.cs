using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AutoNumber.Infrastructure;
using AutoNumber.ViewModels;

namespace AutoNumber.Views
{
    /// <summary>
    /// Interaction logic for Marker.xaml
    /// </summary>
    public partial class Marker : UserControl
    {
        public Marker(MarkerVM markerVM)
        {
            InitializeComponent();
            this.DataContext = markerVM;

            Canvas.SetLeft(this, 0);
            Canvas.SetTop(this, 0);

            // Number labels are draggable, name-table cells are not.
            SetDragLocked(markerVM is not MarkerLabel);

            markerVM.PropertyChanged += MarkerVM_PropertyChanged;
        }

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



        void SetDragLocked(bool locked)
        {
            if (locked)
            {
                MarkerContent.PreviewMouseDown -= MarkerContent_PreviewMouseDown;
                MarkerContent.PreviewMouseMove -= MarkerContent_PreviewMouseMove;
                MarkerContent.PreviewMouseUp -= MarkerContent_PreviewMouseUp;
            }
            else
            {
                MarkerContent.PreviewMouseDown += MarkerContent_PreviewMouseDown;
                MarkerContent.PreviewMouseMove += MarkerContent_PreviewMouseMove;
                MarkerContent.PreviewMouseUp += MarkerContent_PreviewMouseUp;
            }
        }

        private void MarkerVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is MarkerVM vm)
            {
                if (e.PropertyName == nameof(MarkerVM.IsLocked))
                {
                    SetDragLocked(vm.IsLocked);
                }
            }
        }

        #region Moving --------------------------------------------------

        Point? oldMousePosition;
        MainVM? dragMainVM;

        private void MarkerContent_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            oldMousePosition = e.GetPosition(Parent as FrameworkElement);
            dragMainVM = DataContext is MarkerLabel ? VisualTreeHelpers.FindAncestorDataContext<MainVM>(this) : null;

            MarkerContent.CaptureMouse();
            e.Handled = true;
        }

        private void MarkerContent_PreviewMouseMove(object sender, MouseEventArgs e)
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

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && DataContext is MarkerLabel draggedLabel)
                {
                    MoveOtherLabels(draggedLabel, deltaMousePosition);
                }
            }

            e.Handled = true;

        }

        private void MoveOtherLabels(MarkerLabel draggedLabel, Vector delta)
        {
            if (dragMainVM is not { } mainVM)
            {
                return;
            }

            foreach (var person in mainVM.PictureVM.Persons)
            {
                if (person.Label == draggedLabel)
                {
                    continue;
                }

                person.Label.X += delta.X;
                person.Label.Y += delta.Y;
            }
        }

        private void MarkerContent_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            oldMousePosition = null;
            dragMainVM = null;
            MarkerContent.ReleaseMouseCapture();

            if (DataContext is MarkerLabel markerLabel)
            {
                var mainVM = VisualTreeHelpers.FindAncestorDataContext<MainVM>(this);
                if (mainVM?.PictureVM.RowDefinitionSession is not null)
                {
                    markerLabel.Person.Row = mainVM.PictureVM.RowDefinitionSession.ResolveRow(markerLabel.Person);
                    mainVM.LabelManager.Numerate();
                }
            }

            e.Handled = true;
        }

        #endregion
    }
}
