using Emgu.CV;
using NumberIt.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Data;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;


namespace NumberIt.ViewModels
{
    public class ImageModel : BaseViewModel, IDisposable
    {
        public ImageModel()
        {
            //MarkerVMs.CollectionChanged += MarkerVMs_CollectionChanged;
            Labels = CollectionViewSource.GetDefaultView(MarkerVMs);
            Labels.Filter = FilterItems;
        }

        #region Properties ----------------------------------------------------
        public ObservableCollection<MarkerVM> MarkerVMs { get; } = [];
        public ICollectionView Labels { get; }
        private bool FilterItems(object item) => item is MarkerLabel dataItem;

        public Bitmap? Bitmap
        {
            get => _bitmap;
            set => SetProperty(ref _bitmap, value);
        }

        public string Filename { get; set; } = string.Empty;
        public string OutputFilename
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double LabelDiameter
        {
            get => _labelDiameter;
            set
            {
                if (_labelDiameter != value)
                {
                    MarkerLabel.Diameter = (float) value;
                    MarkerLabel.FontSize = getSuitableFontSize(getLargestLabel<MarkerLabel>()); // try to fit th widest label-number into the label
                    SetProperty(ref _labelDiameter, value);
                }
            }
        }

        public Size CanvasSize { get; set; }
        public int ImageWidth
        {
            get => _imageWidth;
            set => SetProperty(ref _imageWidth, value);
        }
        public int ImageHeight
        {
            get => _imageHeight;
            set => SetProperty(ref _imageHeight, value);
        }
        public int PanX
        {
            get => _panX;
            set => SetProperty(ref _panX, value);
        }
        public int PanY
        {
            get => _panY;
            set => SetProperty(ref _panY, value);
        }
        public double Zoom
        {
            get => _zoom;
            set => SetProperty(ref _zoom, value);
        }

        #endregion

        public void Init(string imageFilename)
        {
            Bitmap = new Bitmap(imageFilename);

            Filename = imageFilename;

            MarkerVMs.Clear();

            ImageWidth = Bitmap.Width;
            ImageHeight = Bitmap.Height;

            Zoom = 0.95 * Math.Min((double)CanvasSize.Width / ImageWidth, (double)CanvasSize.Height / ImageHeight);
            PanX = (int)((CanvasSize.Width - ImageWidth * Zoom) / 2);
            PanY = (int)((CanvasSize.Height - ImageHeight * Zoom) / 2);
        }
        public void Dispose() => Bitmap?.Dispose();

        private float getSuitableFontSize(MarkerLabel? largestLabel)
        {
            if (largestLabel == null) return 5;

            using Font font = new Font("Calibri", (float)MarkerLabel.FontSize);
            var d = Analyzer.getCircumscribingDiameter(largestLabel.Number, font);
            
            //using var dummy = new Bitmap(1, 1);      
            //using var g = Graphics.FromImage(dummy); // need a Graphics object for measuring
            //var d = getCircumscribingDiameter(g, largestLabel, l=>l.Number);
            return 1.75f * MarkerLabel.Diameter / d * (float)MarkerLabel.FontSize; // scale the current size up/down, 1.75 is generates a size nicely fitting
        }
        float getCircumscribingDiameter<T>(Graphics g, T label, Func<T, string> measuredProp)
        {
            using Font font = new Font("Calibri", (float)MarkerLabel.FontSize);
            SizeF s = g.MeasureString(measuredProp(label), font);
            return (float) Math.Sqrt(s.Width * s.Width + s.Height * s.Height);
        }
        private T? getLargestLabel<T>()
        {
            var labels = MarkerVMs.OfType<T>();
            if (!labels.Any()) return default;

            using var bmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bmp);

            T largestLabel = labels.First();

            float maxRadius = 0;

            foreach (T label in labels)
            {
                var r = getCircumscribingDiameter<T>(g, label!, l=>"");
                if (r > maxRadius)
                {
                    maxRadius = r;
                    largestLabel = label;
                }
            }
            return largestLabel;
        }


        private void DistributeNames()
        {
            var textLabels = MarkerVMs.OfType<TextLabel>().ToList();

            foreach(var textLabel in textLabels)
            {
                //var largest = getLargestLabel();

            }
        }

        private Bitmap? _bitmap;
        private int _imageHeight, _imageWidth;
        private int _panX, _panY;
        private double _zoom = 0.7;        
        private double _labelDiameter;
    }
}
