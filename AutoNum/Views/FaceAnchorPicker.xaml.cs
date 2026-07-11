using AutoNumber.Model;
using System.Windows;
using System.Windows.Controls;

namespace AutoNumber.Views
{
    /// <summary>
    /// Interaction logic for FaceAnchorPicker.xaml
    ///
    /// Reusable 3x3 grid control for choosing a <see cref="FaceLabelAnchor"/>.
    /// </summary>
    public partial class FaceAnchorPicker : UserControl
    {
        public FaceAnchorPicker()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SelectedAnchorProperty = DependencyProperty.Register(nameof(SelectedAnchor), typeof(FaceLabelAnchor), typeof(FaceAnchorPicker),
            new FrameworkPropertyMetadata(FaceLabelAnchor.BottomCenter, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public FaceLabelAnchor SelectedAnchor
        {
            get { return (FaceLabelAnchor)GetValue(SelectedAnchorProperty); }
            set { SetValue(SelectedAnchorProperty, value); }
        }
    }
}
