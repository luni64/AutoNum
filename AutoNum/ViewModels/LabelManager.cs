﻿using AutoNumber.Model;
using Emgu.CV.Dnn;
using NumberIt.Model;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Linq;

namespace NumberIt.ViewModels
{
    public class LabelManager : BaseViewModel
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
        public void doNumerate(object? _ = null)
        {
            var persons = pvm.Persons;

            double minY = persons.Min(p => p.Label.CenterY);
            double maxY = persons.Max(p => p.Label.CenterY);
            int nrOfRows = (int)Math.Max(1, (maxY - minY) / (d_0 * 1.25));
            double delta = (maxY - minY) / nrOfRows;

            int nr = 1;
            for (int row = 0; row < nrOfRows; row++)
            {
                double lower = minY + row * delta;
                double upper = minY + (row + 1) * delta;
                foreach (var person in persons.Where(p => p.Label.Y >= lower && p.Label.Y <= upper).OrderBy(p => p.Label.X))
                {
                    person.Label.Number = nr;
                    nr++;
                }
            }
            try
            {
                parent.nameManager.refresh();
                parent.nameManager.ShowNames();
            }
            catch { }
        }



        #endregion
        #region Properties --------------------------------------------------

        public int Diameter  // is attached to a slider 0...100. slider values: 0 -> 0.5*d_0,  50 => d_0,  100 => 2*d_0
        {
            get => _diameter;
            set
            {
                SetProperty(ref _diameter, value);
                parent.pictureVM.LabelDiameter = d_0 * (0.5 + 0.0002 * (_diameter * _diameter));
                //TextLabel.TitleFontSize = parent.pictureVM.LabelDiameter / 2;
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
        public void SetLabels(List<Rectangle> faces)
        {
            d_0 = Math.Max(faces.Average(m => m.Width), faces.Average(m => m.Height)) / 2;

            int nr = 1;
            foreach (var face in faces)
            {
                PointF labelPos = new PointF((float)(face.X + face.Width / 2), (float)(face.Y + face.Height * 1.05));
                // pvm.Persons.Add(new Person(0, $"{ExtensionMethods.names[nr++]}", labelPos));
                pvm.Persons.Add(new Person(0, "", labelPos));
            }
            doNumerate();

            parent.nameManager.ShowNames();

            Diameter = 0;
            Diameter = 50; // slider value 
        }

        public LabelManager(MainVM parent)
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

        private ImageModel pvm => parent.pictureVM;
        private double d_0 { get; set; } = 50;

        //private string _number = "";
        private Color _edgeColor = Color.Black, _fontColor = Color.Black, _backgroundColor = Color.White;
        private int _diameter;
        private MainVM parent;
    }
}
