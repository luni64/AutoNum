using Emgu.CV;
using NumberIt.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Windows.Data;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Collections.Specialized;
using System.Security.RightsManagement;


namespace NumberIt.ViewModels
{
    public class ImageModel : BaseViewModel, IDisposable
    {
        public ImageModel()
        {
            //MarkerVMs.CollectionChanged += MarkerVMs_CollectionChanged;
            Labels = CollectionViewSource.GetDefaultView(MarkerVMs);
            Labels.Filter = FilterItems;

            Names = CollectionViewSource.GetDefaultView(MarkerVMs);
            Names.Filter = FilterNames;
        }

        public void AddLabel(MarkerLabel label)
        {
            MarkerVMs.Add(label);
            MarkerVMs.Add(new TextLabel
            {
                Text="new",
                Number =label.Number,
            });
        }

        //private void MarkerVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        //{
        //    switch (e.Action)
        //    {
        //        case NotifyCollectionChangedAction.Add:
        //            foreach(var item in e.NewItems)
        //            {
        //                if(item is MarkerRect face)
        //                {
        //                    MarkerVMs.Add(new MarkerLabel
        //                    {

        //                    });
        //                }
        //            }
        //            break;


                


        //    }

        //}

        #region Properties ----------------------------------------------------
        public ObservableCollection<MarkerVM> MarkerVMs { get; } = [];
        public ICollectionView Labels { get; }
        private bool FilterItems(object item) => item is MarkerLabel dataItem;

        public ICollectionView Names { get; }
        private bool FilterNames(object item) => item is TextLabel dataItem;

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
                    MarkerLabel.Diameter = (float)value;

                    using Font font = new Font(MarkerLabel.fontFamily, (float)MarkerLabel.FontSize);
                    var result = Analyzer.getLargesItem(MarkerVMs.OfType<MarkerLabel>(), i => i.Number, font);
                    MarkerLabel.FontSize = 1.75 * MarkerLabel.Diameter / result.d_max * MarkerLabel.FontSize; // scale the current size up/down, 1.75 is generates a size nicely fitting                
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
            Filename = imageFilename;
            Bitmap = new Bitmap(imageFilename);

            MarkerVMs.Clear();

            ImageWidth = Bitmap.Width;
            ImageHeight = Bitmap.Height;

            Zoom = 0.95 * Math.Min((double)CanvasSize.Width / ImageWidth, (double)CanvasSize.Height / ImageHeight);
            PanX = (int)((CanvasSize.Width - ImageWidth * Zoom) / 2);
            PanY = (int)((CanvasSize.Height - ImageHeight * Zoom) / 2);
        }
        public void Dispose() => Bitmap?.Dispose();

        //private float getSuitableFontSize(float diameter)
        //{
        //    if (diameter == 0) return 5;         
        //    return 1.75f * MarkerLabel.Diameter / diameter * (float)MarkerLabel.FontSize; // scale the current size up/down, 1.75 is generates a size nicely fitting
        //}
        //float getCircumscribingDiameter<T>(Graphics g, T label, Func<T, string> measuredProp)
        //{
        //    using Font font = new Font("Calibri", (float)MarkerLabel.FontSize);
        //    SizeF s = g.MeasureString(measuredProp(label), font);
        //    return (float) Math.Sqrt(s.Width * s.Width + s.Height * s.Height);
        //}
        //private T? getLargestLabel<T>()
        //{
        //    var labels = MarkerVMs.OfType<T>();
        //    if (!labels.Any()) return default;

        //    using var bmp = new Bitmap(1, 1);
        //    using var g = Graphics.FromImage(bmp);

        //    T largestLabel = labels.First();

        //    float maxRadius = 0;

        //    foreach (T label in labels)
        //    {
        //        var r = getCircumscribingDiameter<T>(g, label!, l=>"");
        //        if (r > maxRadius)
        //        {
        //            maxRadius = r;
        //            largestLabel = label;
        //        }
        //    }
        //    return largestLabel;
        //}


        private void DistributeNames()
        {
            var textLabels = MarkerVMs.OfType<TextLabel>().ToList();

            foreach (var textLabel in textLabels)
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
