using System.Drawing;

namespace AutoNumber.ViewModels
{
    public class Person : BaseViewModel
    {
        public MarkerLabel Label { get; set; }
        public TextLabel Name { get; set; }

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
        public string FullName => $"{Label.Number}) {Name.Text}";

        public override string ToString() => FullName;
    }
}
