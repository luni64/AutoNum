using AutoNumber.Model;
using Emgu.CV.Shape;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Permissions;
using System.Text;


namespace AutoNumber.ViewModels
{
    public class ImageModel : BaseViewModel, IDisposable
    {
        public MainVM parent;

        public ImageModel(MainVM parent)
        {
            this.parent = parent;           
        }

        
        #region Properties ----------------------------------------------------
  
        public ObservableCollection<Person> Persons { get; } = [];

        public Bitmap? Bitmap
        {
            get => _bitmap;
            set => SetProperty(ref _bitmap, value);
        }

        public string OriginalImageFilename {  get; set; }   
        public string AutoNumFilename { get; set; }

     //   public string InputFile { get; set; } = string.Empty;
        public string OutputFile { get; set; } = string.Empty;       

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
            MarkerLabel.FontSize = md.LabelsFont.Size;
           
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
    }
}
