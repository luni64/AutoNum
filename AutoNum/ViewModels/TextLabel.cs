using System.ComponentModel;
using System.Drawing;

namespace AutoNumber.ViewModels
{
    public class TextLabel : MarkerVM
    {
        public static TextStyle Style { get; set; } = new();

        #region properties ------------------------------------------------------
        public string? Text
        {
            get => _text == "" ? null : _text;
            set
            {
                SetProperty(ref _text, value);
                Person.OnPropertyChanged("FullName");
            }
        }

        // Instance properties delegating to the shared style
        public FontFamily FontFamily => Style.FontFamily;
        public Color FontColor => Style.FontColor;
        public double FontSize => Style.FontSize;
        #endregion

        public override string ToString() => $"TL {Text}";

        public TextLabel(Person person)
        {
            this.Person = person;
            PropertyChangedEventManager.AddHandler(Style, OnStyleChanged, string.Empty);
        }

        private void OnStyleChanged(object? sender, PropertyChangedEventArgs e)
            => OnPropertyChanged(e.PropertyName!);

        public Person Person { get; }

        #region private fields -----------------------------------------------
        private string? _text;
        #endregion
    }
}