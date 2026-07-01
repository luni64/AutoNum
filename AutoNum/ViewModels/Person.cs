using System.Drawing;

namespace AutoNumber.ViewModels
{
    public class Person : BaseViewModel
    {
        public MarkerLabel Label { get; set; }
        public TextLabel Name { get; set; }

        public int Row
        {
            get => _row;
            set
            {
                if (!EqualityComparer<int>.Default.Equals(_row, value))
                {
                    SetProperty(ref _row, value);
                    OnPropertyChanged(nameof(FullName));
                }
            }
        }

        public Person(int nr, string name, PointF labelPosition)
        {
            Label = new MarkerLabel(this)
            {
                Number = nr,
                CenterX = labelPosition.X,
                CenterY = labelPosition.Y,
                Visible = true,
                IsLocked = false,
            };
            Name = new TextLabel(this)
            {
                Text = name,
                Visible = false,
                IsLocked = true,
            };
        }
        public bool RowPreviewActive
        {
            get => _rowPreviewActive;
            set => SetProperty(ref _rowPreviewActive, value);
        }

        public Color RowPreviewColor
        {
            get => _rowPreviewColor;
            set => SetProperty(ref _rowPreviewColor, value);
        }

        public string FullName => Row > 0 ? $"R{Row}: {Label.Number}) {Name.Text}" : $"{Label.Number}) {Name.Text}";

        public PointF GetRowAnchorPoint()
        {
            var halfDiameter = Label.Diameter / 2.0;
            return new PointF((float)(Label.CenterX + halfDiameter), (float)(Label.CenterY + halfDiameter));
        }

        public override string ToString() => FullName;

        private int _row;
        private bool _rowPreviewActive;
        private Color _rowPreviewColor = Color.White;
    }
}
