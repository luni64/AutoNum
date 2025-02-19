using System.Drawing;

namespace NumberIt.ViewModels
{
    public class LabelsVM : WizardStep
    {
        #region Commands ---------------------------------------------------------

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
        public Color FontColor
        {
            get => _fontColor;
            set
            {
                SetProperty(ref _fontColor, value);                
                MarkerLabel.FontColor = _fontColor;
            }
        }
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                SetProperty(ref _backgroundColor, value);
                MarkerLabel.BackgroundColor = value;
            }
        }
        public Color EdgeColor
        {
            get => _edgeColor;
            set
            {
                SetProperty(ref _edgeColor, value);
                MarkerLabel.EdgeColor = value;
            }
        }

        #endregion

        public LabelsVM(MainVM parent)
        {
            this.parent = parent;           
        }


        private void move(double horizontal, double vertical)
        {
            var labels = pvm.MarkerVMs.OfType<MarkerLabel>().OrderBy(m => m.X).ToList();
            foreach (var label in labels)
            {
                label.X += horizontal;
                label.Y += vertical;
            }
        }


        private void SetLabels()
        {
            // set current colors            
            MarkerLabel.BackgroundColor = BackgroundColor;
            MarkerLabel.EdgeColor = EdgeColor;
            MarkerLabel.FontColor = FontColor;

            var faces = pvm.MarkerVMs.OfType<MarkerRect>().OrderBy(m => m.X).ToList();

            d_0 = Math.Max(faces.Average(m => m.W), faces.Average(m => m.H)) / 2;

            // split into rows
            double minY = faces.Min(m => m.Y);
            double maxY = faces.Max(m => m.Y);
            int nrOfRows = (int)Math.Max(1, (maxY - minY) / (d_0 * 1.25));
            double delta = (maxY - minY) / nrOfRows;

            int nr = 1;
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
                        Number = (nr++).ToString()
                    });
                }
                Diameter = 50; // slider value 
            }
        }

        public override void Enter(object? o)
        {
            foreach (var marker in pvm.MarkerVMs)  // hide face markers, show label markers
            {
                if (marker is MarkerLabel) marker.visible = true;
                else marker.visible = false;
            }

            if (!pvm.MarkerVMs.Any(m => m is MarkerLabel))  //  calculate labels from faces only once, to not overwrite adjustments
            {
                SetLabels();
            }
        }

        private ImageModel pvm => parent.pictureVM;
        private double d_0 { get; set; } = 50;

        private string _number = "";
        private Color _edgeColor = Color.Black, _fontColor = Color.Black, _backgroundColor = Color.White;
        private int _diameter;
        private MainVM parent;
    }
}
