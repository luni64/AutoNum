using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                visible = true,
                isLocked = false,
            };
            Name = new TextLabel(this)
            {
                Text = name,
                visible = false,
                isLocked = true,
            };                   
        }
        public string FullName => $"{Label.Number}) {Name.Text}";

        public override string ToString() => FullName;
    }
}
