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
            => OnPropertyChanged(e.PropertyName!);

        #region properties ------------------------------------------------------
        override public double X
        {
            get => CenterX + W / 2;
            set
            {
                CenterX = value - W / 2;
                OnPropertyChanged(nameof(X));
            }
        }
        override public double Y
        {
            get => CenterY + H / 2;
            set
            {
                CenterY = value + H / 2;
                OnPropertyChanged(nameof(Y));
            }
        }

        public int Number
        {
            get => _nr;
            set
            {
                SetProperty(ref _nr, value);
                Person.OnPropertyChanged("FullName");
            }
        }
        public string? Name
        {
            get => _name == "" ? null : _name;
            set => SetProperty(ref _name, value);
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

        public override string ToString() => $"LB: {Number}-{Name}";

        #region private fields -----------------------------------------------
        private int _nr;
        private string? _name;
        private double _centerX;
        private double _centerY;
        #endregion
    }
}
