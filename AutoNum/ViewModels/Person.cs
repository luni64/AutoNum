using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumberIt.ViewModels
{
    public class Person : BaseViewModel
    {
        public MarkerLabel Label { get; set; }
        public TextLabel Name { get; set; }

        public Person(int nr, string name, PointF labelPosition)
        {
            Label = new MarkerLabel
            {
                Number = nr.ToString(),
                CenterX = labelPosition.X,
                CenterY = labelPosition.Y,
                visible = true,
                isLocked = false,
            };
            Name = new TextLabel
            {
                Text = name,
                visible = false,
                isLocked = true,
            };            
        }
    }
}
