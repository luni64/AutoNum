using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NumberIt.Views
{
    /// <summary>
    /// Interaction logic for FontManager.xaml
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


        public static readonly DependencyProperty SelectedFontSizeProperty = DependencyProperty.Register(nameof(SelectedFontSize), typeof(double), typeof(FontManager), 
            new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double SelectedFontSize
        {
            get { return (double)GetValue(SelectedFontSizeProperty); }
            set { SetValue(SelectedFontSizeProperty, value); }
        }


    }
}
