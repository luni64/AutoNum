//using System.Windows;
using Emgu.CV.Text;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
//using Color = System.Drawing.Color;

namespace NumberIt.ViewModels
{
    public class LabelsVM : WizardStep
    {
        #region commands ---------------------------------------------------------
        RelayCommand? _cmdMoveLabel;
        public RelayCommand cmdMoveLabel => _cmdMoveLabel ??= new RelayCommand(doMoveLabel);
        void doMoveLabel(object? o)
        {
            double delta = d_0 / 50;
            switch (o as string)
            {
                case "up":
                    move(0, -delta);
                    break;
                case "down":
                    move(0, delta);
                    break;
                case "left":
                    move(-delta, 0);
                    break;
                case "right":
                    move(delta, 0);
                    break;
            }
        }

        RelayCommand? _cmdNumerate;
        public RelayCommand cmdNumerate => _cmdNumerate ??= new RelayCommand(doNumerate);
        void doNumerate(object? o)
        {
            

            var labels = pvm.MarkerVMs.OfType<MarkerLabel>().OrderBy(m => m.X).ToList();

            double minY = labels.Min(m => m.Y);
            double maxY = labels.Max(m => m.Y);
            int nrOfRows = (int)Math.Max(1, (maxY - minY) / (d_0 * 1.25));
            double delta = (maxY - minY) / nrOfRows;

            int nr = 1;
            for (int i = 0; i < nrOfRows; i++)
            {
                double lower = minY + i * delta;
                double upper = minY + (i + 1) * delta;
                foreach (var lable in labels.Where(f => f.Y >= lower && f.Y <= upper))
                {
                    lable.Number = (nr++).ToString();
                }
            }
        }

        #endregion

        #region Properties --------------------------------------------------

        public string Number
        {
            get => _number;
            set => SetProperty(ref _number, value);
        }
        public int Diameter  // will be attached to a slider 0...100. slider values: 0 -> 0.5*d_0,  50 => d_0,  100 => 2*d_0
        {
            get => _diameter;
            set
            {
                SetProperty(ref _diameter, value);
                MarkerLabel.Diameter = d_0 * (0.5 + 0.0002 * (_diameter * _diameter));     
                
                
            }
        }
        public Color? FontColor
        {
            get => _fontColor;
            set
            {
                SetProperty(ref _fontColor, value);
                MarkerLabel.ForegroundBrush = new SolidColorBrush(_fontColor!.Value);                
            }
        }
        public Color? BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                SetProperty(ref _backgroundColor, value);
                MarkerLabel.BackgroundBrush = new SolidColorBrush(_backgroundColor!.Value);                
            }
        }
        public Color? EdgeColor
        {
            get => _edgeColor;
            set
            {
                SetProperty(ref _edgeColor, value);
                MarkerLabel.EdgeBrush = new SolidColorBrush(_edgeColor!.Value);                
            }
        }
        
        #endregion

        public LabelsVM(MainVM parent)
        {
            this.parent = parent;
            //pvm = parent.pictureVM;

            Diameter = 0;
        }


        void move(double horizontal, double vertical)
        {
            var labels = pvm.MarkerVMs.OfType<MarkerLabel>().OrderBy(m => m.X).ToList();
            foreach (var label in labels)
            {
                label.X += horizontal;
                label.Y += vertical;
            }
        }
        
        int _diameter;

        private void SetLables()
        {
            var faces = pvm.MarkerVMs.OfType<MarkerRect>().OrderBy(m => m.X).ToList();
            
            d_0 = Math.Max(faces.Average(m => m.W), faces.Average(m => m.H)) / 2;
            double minY = faces.Min(m => m.Y);
            double maxY = faces.Max(m => m.Y);
            int nrOfRows = (int)Math.Max(1, (maxY - minY) / (d_0 * 1.25));
            double delta = (maxY - minY) / nrOfRows;

            MarkerLabel.ForegroundBrush = new SolidColorBrush(FontColor!.Value);
            MarkerLabel.BackgroundBrush = new SolidColorBrush(BackgroundColor!.Value);
            MarkerLabel.EdgeBrush = new SolidColorBrush(EdgeColor!.Value);

            int j = 1;
            for (int i = 0; i < nrOfRows; i++)
            {
                double lower = minY + i * delta;
                double upper = minY + (i + 1) * delta;

                foreach (var face in faces.Where(f => f.Y >= lower && f.Y <= upper))
                {
                    pvm.MarkerVMs.Add(new MarkerLabel
                    {
                        CenterX = face.X + face.W / 2 - d_0 / 2,
                        CenterY = face.Y + face.H,                        
                        visible = true,
                        Number = (j++).ToString()
                    });

                    //var ml = pvm.MarkerVMs.Last() as MarkerLabel;

                    //Trace.WriteLine($"mm {(pvm.MarkerVMs.Last() as MarkerLabel).CenterX}");
                }

                Diameter = 50; // slider value 
            }



            //foreach (var label in pvm.MarkerVMs.Where(m => m is MarkerRect).ToList())
            //{
            //    pvm.MarkerVMs.Add(new MarkerLabel
            //    {
            //        X = label.X + label.W / 2 - d_0/4,
            //        Y = label.Y + label.H,
            //        H = d_0/2,
            //        W = d_0/2,
            //        visible = true,
            //        Number = (i++).ToString()
            //    });
            //}
        }

        public override void Enter(object? o)
        {
            foreach (var marker in pvm.MarkerVMs)
            {
                if (marker is MarkerLabel) marker.visible = true;
                else marker.visible = false;
            }

            //pvm.MarkerVMs.OfType<MarkerLabel>().ToList().ForEach(m => pvm.MarkerVMs.Remove(m));

            if (!pvm.MarkerVMs.Any(m => m is MarkerLabel))  //  calculate labels from faces only once, to not overwrite adjustments
            {
                SetLables();
            }
        }

        private ImageModel pvm => parent.pictureVM;
        private double d_0 = 50;
        private string _number = "";
        private MainVM parent;
        private Color? _edgeColor = Colors.Black;
        private Color? _fontColor = Colors.Black;
        private Color? _backgroundColor = Colors.White;
    }
}
