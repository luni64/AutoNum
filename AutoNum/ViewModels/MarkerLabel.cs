using System.ComponentModel;
using System.Drawing;

namespace AutoNumber.ViewModels
{
    public class MarkerLabel : MarkerVM
    {
        public static LabelStyle Style { get; set; } = new();

        public MarkerLabel(Person person)
        {
            this.Person = person;
            PropertyChangedEventManager.AddHandler(Style, OnStyleChanged, string.Empty);
        }

        private void OnStyleChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName!);

            // X/Y are derived from Diameter (to keep the circle centered on CenterX/CenterY),
            // so a size change must also refresh the bound Canvas position.
            if (e.PropertyName == nameof(LabelStyle.Diameter))
            {
                OnPropertyChanged(nameof(X));
                OnPropertyChanged(nameof(Y));
            }
        }

        #region properties ------------------------------------------------------
        /// <summary>
        /// Canvas.Left equivalent (top-left of the rendered circle), derived from the
        /// true center (CenterX/CenterY) and the current diameter.
        /// </summary>
        override public double X
        {
            get => CenterX - Diameter / 2.0;
            set
            {
                CenterX = value + Diameter / 2.0;
                OnPropertyChanged(nameof(X));
            }
        }
        /// <summary>
        /// Canvas.Top equivalent (top-left of the rendered circle), derived from the
        /// true center (CenterX/CenterY) and the current diameter.
        /// </summary>
        override public double Y
        {
            get => CenterY - Diameter / 2.0;
            set
            {
                CenterY = value + Diameter / 2.0;
                OnPropertyChanged(nameof(Y));
            }
        }

        public int Number
        {
            get => _nr;
            set
            {
                SetProperty(ref _nr, value);
                Person.OnPropertyChanged(nameof(Person.FullName));
            }
        }

        public double CenterX
        {
            get => _centerX;
            set => SetProperty(ref _centerX, value);
        }

        public double CenterY
        {
            get => _centerY;
            set => SetProperty(ref _centerY, value);
        }

        // Instance properties delegating to the shared style
        public float Diameter => Style.Diameter;
        public double FontSize => Style.FontSize;
        public FontFamily FontFamily => Style.FontFamily;
        public Color FontColor => Style.FontColor;
        public Color EdgeColor => Style.EdgeColor;
        public Color BackgroundColor => Style.BackgroundColor;

        public Person Person { get; }

        #endregion

        public override string ToString() => $"LB: {Number}";

        #region private fields -----------------------------------------------
        private int _nr;
        private double _centerX;
        private double _centerY;
        #endregion
    }
}
