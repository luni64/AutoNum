using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoNumber.Views
{
    /// <summary>
    /// Interaction logic for FontManager.xaml
    /// 
    /// This control encapsulates slider-based font size control. It accepts a scale property
    /// (0.25–4.0) from the DataContext and handles conversion to/from slider position (0–1)
    /// via the SliderToScaleConverter.
    /// </summary>
    public partial class FontManager : UserControl
    {
        public FontManager()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty FontColorProperty = DependencyProperty.Register(nameof(FontColor), typeof(Color), typeof(FontManager),
           new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Color FontColor
        {
            get { return (Color)GetValue(FontColorProperty); }
            set { SetValue(FontColorProperty, value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register(nameof(BackgroundColor), typeof(Color), typeof(FontManager),
            new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Color BackgroundColor
        {
            get { return (Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }


        public static readonly DependencyProperty SelectedScaleProperty = DependencyProperty.Register(nameof(SelectedScale), typeof(double), typeof(FontManager), 
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedScale_Changed));

        private static void SelectedScale_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// Scale factor (0.25–4.0). The scale applied to the base font size.
        /// When bound to the Slider, a converter translates between slider position (0–1) and scale.
        /// </summary>
        public double SelectedScale
        {
            get { return (double)GetValue(SelectedScaleProperty); }
            set { SetValue(SelectedScaleProperty, value); }
        }

        public static readonly DependencyProperty EdgeColorProperty = DependencyProperty.Register(nameof(EdgeColor), typeof(Color), typeof(FontManager),
            new FrameworkPropertyMetadata(Colors.Transparent, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Color EdgeColor
        {
            get { return (Color)GetValue(EdgeColorProperty); }
            set { SetValue(EdgeColorProperty, value); }
        }

        public static readonly DependencyProperty ShowEdgeColorProperty = DependencyProperty.Register(nameof(ShowEdgeColor), typeof(bool), typeof(FontManager),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool ShowEdgeColor
        {
            get { return (bool)GetValue(ShowEdgeColorProperty); }
            set { SetValue(ShowEdgeColorProperty, value); }
        }
    }
}
