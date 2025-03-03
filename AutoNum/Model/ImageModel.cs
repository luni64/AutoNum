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
using System.Diagnostics;


namespace NumberIt.ViewModels
{
    public class ImageModel : BaseViewModel, IDisposable
    {
        public MainVM parent;

        public ImageModel(MainVM parent)
        {
            this.parent = parent;
            //MarkerVMs.CollectionChanged += MarkerVMs_CollectionChanged;
            //Labels = CollectionViewSource.GetDefaultView(MarkerVMs);
            //Labels.Filter = FilterItems;

            //Names = CollectionViewSource.GetDefaultView(MarkerVMs);
            //Names.Filter = FilterNames;

         
        }



        //private void MarkerVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        //{
        //    PersonsView.Refresh();


        //}

        public void RemoveLabel(MarkerLabel label)
        {
            throw new NotImplementedException();
            //var tl = MarkerVMs.OfType<TextLabel>().FirstOrDefault(m => m.Number == label.Number.ToString());
            //if (tl == null)
            //    Trace.WriteLine("------NULL---------");
            //MarkerVMs.Remove(tl);
            //MarkerVMs.Remove(label);
            ////MarkerVMs.Add(new TextLabel
            ////{
            ////    isLocked = true,
            ////    Text = "new",
            ////    Number = label.Number,
            ////});
        }

        public void AddLabel(MarkerLabel label)
        {
            throw new NotImplementedException();
            //MarkerVMs.Add(label);
            //MarkerVMs.Add(new TextLabel
            //{
            //    isLocked = true,
            //    Text = $"new {label.Number}",
            //    Number = label.Number.ToString(),
            //});
        }


        #region Properties ----------------------------------------------------
        public ObservableCollection<MarkerVM> MarkerVMs { get; } = [];
        public ObservableCollection<Person> Persons { get; } = [];
        //public ICollectionView PersonsView { get; }
        //public ICollectionView Labels { get; }
        //private bool FilterItems(object item) => item is MarkerLabel dataItem;

        //public ICollectionView Names { get; }
        //private bool FilterNames(object item) => item is TextLabel dataItem;

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

                    using Font font = new Font(MarkerLabel.FontFamily, (float)MarkerLabel.FontSize);
                    var result = Analyzer.getLargesItem(Persons, p => p.Label.Number.ToString(), font);
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

        double _namesRegionHeight;
        public double NamesRegionHeight
        {
            get => _namesRegionHeight;
            set => SetProperty(ref _namesRegionHeight, value);
        }

        double _titleRegionHeight = 300;
        public double TitleRegionHeight
        {
            get => _titleRegionHeight;
            set => SetProperty(ref _titleRegionHeight, value);
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
            Persons.Clear();

            ImageWidth = Bitmap.Width;
            ImageHeight = Bitmap.Height;

            Zoom = 0.95 * Math.Min((double)CanvasSize.Width / ImageWidth, (double)CanvasSize.Height / ImageHeight);
            PanX = (int)((CanvasSize.Width - ImageWidth * Zoom) / 2);
            PanY = (int)((CanvasSize.Height - ImageHeight * Zoom) / 2);

            MarkerLabel.BackgroundColor = parent.labelManager.BackgroundColor;
            MarkerLabel.EdgeColor = parent.labelManager.EdgeColor;
            MarkerLabel.FontColor = parent.labelManager.FontColor;
            MarkerLabel.FontSize = 12;
            NameManager.DefaultFontSize = 80;
        }
        public void Dispose() => Bitmap?.Dispose();

        //private float getSuitableFontSize(float diameter)
        //{
        //    if (diameter == 0) return 5;         
        //    return 1.75f * MarkerLabel.Diameter / diameter * (float)MarkerLabel.TitleFontSize; // scale the current size up/down, 1.75 is generates a size nicely fitting
        //}
        //float getCircumscribingDiameter<T>(Graphics g, T label, Func<T, string> measuredProp)
        //{
        //    using Font font = new Font("Calibri", (float)MarkerLabel.TitleFontSize);
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


        //private void DistributeNames()
        //{
        //    var textLabels = MarkerVMs.OfType<TextLabel>().ToList();

        //    foreach (var textLabel in textLabels)
        //    {
        //        //var largest = getLargestLabel();

        //    }
        //}

        private Bitmap? _bitmap;
        private int _imageHeight, _imageWidth;
        private int _panX, _panY;
        private double _zoom = 0.7;
        private double _labelDiameter;
    }
}
