using AutoNumber.Model;
using System.Collections.ObjectModel;
using System.Drawing;


namespace AutoNumber.ViewModels
{
    public class ImageModel : BaseViewModel, IDisposable
    {

        public ImageModel(MainVM parent)
        {
            this.parent = parent;
        }


        #region Properties ----------------------------------------------------
        public Bitmap? Bitmap
        {
            get => _bitmap;
            set => SetProperty(ref _bitmap, value);
        }
        public MainVM parent { get; }
        public string OriginalImageFilename { get; set; } = string.Empty;
        public ObservableCollection<Person> Persons { get; } = [];
        public double LabelDiameter
        {
            get => _labelDiameter;
            set
            {
                if (_labelDiameter != value)
                {
                    MarkerLabel.Diameter = (float)value;

                    using Font font = new Font(MarkerLabel.FontFamily, (float)MarkerLabel.FontSize);
                    var result = Analyzer.getLargestItem(Persons, p => p.Label.Number.ToString(), font);
                    MarkerLabel.FontSize = 1.5 * MarkerLabel.Diameter / result.d_max * MarkerLabel.FontSize; // scale the current size up/down, 1.75 generates a size nicely fitting                
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
        public double NamesRegionHeight
        {
            get => _namesRegionHeight;
            set => SetProperty(ref _namesRegionHeight, value);
        }
        public double TitleRegionHeight
        {
            get => _titleRegionHeight;
            set => SetProperty(ref _titleRegionHeight, value);
        }

        // position and size of the image on the canvas
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

        public void InitFromMetadata(AutoNumMetaData_V1 md)
        {
            ImageWidth = Bitmap?.Width ?? 0;
            ImageHeight = Bitmap?.Height ?? 0;
            Zoom = 0.95 * Math.Min((double)CanvasSize.Width / ImageWidth, (double)CanvasSize.Height / ImageHeight);
            PanX = (int)((CanvasSize.Width - ImageWidth * Zoom) / 2);
            PanY = (int)((CanvasSize.Height - ImageHeight * Zoom) / 2);

            Persons.Clear();

            foreach (var p in md.Persons)
            {
                this.Persons.Add(new Person(p.Label.Number, p.Name.Text, new PointF(p.Label.CenterX, p.Label.CenterY)));
            }

            NameManager.DefaultFontSize = 80;

            parent.labelManager.BackgroundColor = Color.FromArgb(md.LabelsFont.background);
            parent.labelManager.FontColor = Color.FromArgb(md.LabelsFont.foreground);
            parent.labelManager.d_0 = md.LabelsFont.Size * 0.75; /// Hack
            MarkerLabel.FontSize = md.LabelsFont.Size*0.5;
            LabelDiameter = md.LabelsFont.Size * 0.75;
           

            parent.titleManager.BackgroundColor = Color.FromArgb(md.TitleFont.background);
            parent.titleManager.TitleFontColor = Color.FromArgb(md.TitleFont.foreground);
            parent.titleManager.Title = md.Title;
            if (!string.IsNullOrEmpty(md.Title)) parent.titleManager.IsEnabled = true;

            parent.nameManager.BackgroundColor = Color.FromArgb(md.NamesFont.background);
            parent.nameManager.FontColor = Color.FromArgb(md.NamesFont.foreground);
            parent.nameManager.FontFamily = new FontFamily(md.NamesFont.Family);
            if (Persons.Count > 0) parent.nameManager.IsEnabled = true;

            parent.labelManager.doNumerate();
            parent.nameManager.ShowNames();

            parent.labelManager.Diameter = 0;
            parent.labelManager.Diameter = 50; // slider value 
        }

        public void Init()
        {
            Persons.Clear();

            ImageWidth = Bitmap?.Width ?? 0;
            ImageHeight = Bitmap?.Height ?? 0;

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

        private Bitmap? _bitmap;
        private int _imageHeight, _imageWidth;
        private int _panX, _panY;
        private double _zoom = 0.7;
        private double _labelDiameter;
        private double _namesRegionHeight;
        private double _titleRegionHeight = 300;
    }
}
