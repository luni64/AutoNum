using AutoNumber.ViewModels;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoNumber.Model
{
    public class AutoNumFont
    {
        [JsonPropertyName("foreground")]
        public int Foreground { get; set; } = Color.Black.ToArgb();
        [JsonPropertyName("background")]
        public int Background { get; set; } = Color.White.ToArgb();
        public string Family { get; set; } = string.Empty;
        public double Size { get; set; }

        public AutoNumFont(Color fg, Color bg, string family, double size)
        {
            Foreground = fg.ToArgb();
            Background = bg.ToArgb();
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
        public double LabelsSize { get; set; } = double.NaN;
        public AutoNumFont NamesFont { get; set; } = new AutoNumFont();
        public bool? NamesEnabled { get; set; }
        public string ImageId { get; set; } = string.Empty;
        public AutoNumFont ImageIdFont { get; set; } = new AutoNumFont();
        public bool? ImageIdEnabled { get; set; }
        public AutoNumFont TitleFont { get; set; } = new AutoNumFont();
        public bool? TitleEnabled { get; set; }
        public string Title { get; set; } = string.Empty;
        public AutoNumFont ImageInfoFont { get; set; } = new AutoNumFont();
        public bool? ImageInfoEnabled { get; set; }
        public string ImageInfo { get; set; } = string.Empty;
        public List<AutoNumPerson> Persons { get; set; } = [];

        public AutoNumMetaData_V1(ImageVM model, LabelManager lm, NameManager nm, TitleManager tm, ImageInfoManager iim, ImageIdManager idm)
        {
            Created = DateTime.Now;
            OriginalImage = model.OriginalImageFilename;
            AutoNumImage = string.Empty;
            Title = tm.Title;
            ImageInfo = iim.ImageInfo;

            LabelsFont = new AutoNumFont(MarkerLabel.Style.FontColor, lm.BackgroundColor, MarkerLabel.Style.FontFamily.Name, MarkerLabel.Style.FontSize);
            LabelsSize = MarkerLabel.Style.Diameter;
            NamesFont = new AutoNumFont(nm.FontColor, nm.BackgroundColor, nm.FontFamily.Name, TextLabel.Style.FontSize);
            NamesEnabled = nm.IsEnabled;
            ImageId = idm.ImageId;
            ImageIdFont = new AutoNumFont(idm.FontColor, idm.BackgroundColor, idm.FontFamily.Name, idm.FontSize);
            ImageIdEnabled = idm.IsEnabled;
            TitleFont = new AutoNumFont(tm.TitleFontColor, tm.BackgroundColor, tm.TitleFontFamily.Name, tm.TitleFontSize);
            TitleEnabled = tm.IsEnabled;
            ImageInfoFont = new AutoNumFont(iim.ImageInfoFontColor, iim.BackgroundColor, iim.ImageInfoFontFamily.Name, iim.ImageInfoFontSize);
            ImageInfoEnabled = iim.IsEnabled;

            foreach (var person in model.Persons)
            {
                Persons.Add(new AutoNumPerson(person));
            }
        }
        public AutoNumMetaData_V1() { }


        public string ToJson() => JsonSerializer.Serialize(this, this.GetType(), new JsonSerializerOptions { WriteIndented = true });

        public static bool FromJson(string json, out AutoNumMetaData_V1? MetaData)
        {
            MetaData = null;

            try
            {
                if (!json.GetVersion(out string version))
                    return false;

                MetaData = version switch
                {
                    "V3" => JsonSerializer.Deserialize<AutoNumMetaData_V3>(json),
                    "V2" => JsonSerializer.Deserialize<AutoNumMetaData_V2>(json),
                    "V1" => JsonSerializer.Deserialize<AutoNumMetaData_V1>(json),
                    _ => null,
                };

                return MetaData is not null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to parse AutoNum metadata: {ex}");
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
        public static bool GetVersion(this string json, out string version)
        {
            try
            {
                var r = JsonSerializer.Deserialize<VersionTest>(json);
                version = (r != null && r.Creator == "AutoNumber" && !string.IsNullOrEmpty(r.Version)) ? r.Version : "";
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to parse version from metadata: {ex}");
                version = string.Empty;
            }

            return version != string.Empty;
        }
    }
}
