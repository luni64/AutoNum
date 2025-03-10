using AutoNumber.ViewModels;
using System.Drawing;
using System.Text.Json;

namespace AutoNumber.Model
{
    public class AutoNumFont
    {
        public int foreground { get; set; } = Color.Black.ToArgb();
        public int background { get; set; } = Color.White.ToArgb();
        public string Family { get; set; } = string.Empty;
        public double Size { get; set; }

        public AutoNumFont(Color fg, Color bg, string family, double size)
        {
            foreground = fg.ToArgb();
            background = bg.ToArgb();
            Family = family;
            Size = size;
        }
        public AutoNumFont() { }

    }

    public class Name
    {
        public string Text { get; set; } = string.Empty;
        public double PosX { get; set; }
        public double PosY { get; set; }
        public Name(TextLabel name)
        {
            Text = name.Text ?? "";
            PosX = name.X;
            PosY = name.Y;
        }
        public Name() { }
    }

    public class Label
    {
        public int Number { get; set; }
        public float CenterX { get; set; }
        public float CenterY { get; set; }

        public Label(MarkerLabel label)
        {
            Number = label.Number;
            CenterX = (float)label.CenterX;
            CenterY = (float)label.CenterY;
        }
        public Label() { }
    }

    public class AutoNumPerson
    {
        public Label Label { get; set; } = null!;
        public Name Name { get; set; } = null!;

        public AutoNumPerson(Person person)
        {
            Label = new(person.Label);
            Name = new(person.Name);
        }

        public AutoNumPerson() { }
    }

    public class AutoNumMetaData_V1
    {
        public DateTime Created { get; set; }
        public string Creator { get; set; } = "AutoNumber";
        public string Version { get; set; } = "V1";
        public string OriginalImage { get; set; } = string.Empty;
        public string AutoNumImage { get; set; } = string.Empty;
        public AutoNumFont LabelsFont { get; set; } = new AutoNumFont();
        public AutoNumFont NamesFont { get; set; } = new AutoNumFont();
        public AutoNumFont TitleFont { get; set; } = new AutoNumFont();
        public string Title { get; set; } = string.Empty;
        public List<AutoNumPerson> Persons { get; set; } = [];

        public AutoNumMetaData_V1(ImageModel model)
        {
            Created = DateTime.Now;
            OriginalImage = model.OriginalImageFilename;
            AutoNumImage = string.Empty;
            Title = model.parent.titleManager.Title;

            var lm = model.parent.labelManager;
            var nm = model.parent.nameManager;
            var tm = model.parent.titleManager;
            LabelsFont = new AutoNumFont(MarkerLabel.FontColor, lm.BackgroundColor, MarkerLabel.FontFamily.Name, MarkerLabel.FontSize);
            NamesFont = new AutoNumFont(nm.FontColor, nm.BackgroundColor, nm.FontFamily.Name, TextLabel.FontSize);
            TitleFont = new AutoNumFont(tm.TitleFontColor, tm.BackgroundColor, tm.TitleFontFamily.Name, tm.TitleFontSize);

            foreach (var person in model.Persons)
            {
                Persons.Add(new AutoNumPerson(person));
            }
        }
        public AutoNumMetaData_V1() { }


        public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

        public static bool fromJson(string json, out AutoNumMetaData_V1? MetaData)
        {
            MetaData = null;

            try
            {
                if (json.getVersion(out string version) && version == "V1")
                {
                    MetaData = JsonSerializer.Deserialize<AutoNumMetaData_V1>(json);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    public class VersionTest
    {
        public string Creator { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }

    public static class Ext
    {
        public static bool getVersion(this string json, out string version)
        {
            try
            {
                var r = JsonSerializer.Deserialize<VersionTest>(json);
                version = (r != null && r.Creator == "AutoNumber" && !string.IsNullOrEmpty(r.Version)) ? r.Version : "";
            }
            catch
            {
                version = string.Empty;
            }

            return version != string.Empty;
        }
    }
}
