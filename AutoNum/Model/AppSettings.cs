using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace AutoNumber.Model;

public class AppSettings
{
    public int SchemaVersion { get; set; }
    public bool DefaultNamesEnabled { get; set; } = false;
    public bool DefaultTitleEnabled { get; set; } = false;

    public int DefaultLabelDiameterSlider { get; set; } = (int)SizingModel.SliderPercentDefault;
    public double DefaultNamesFontSlider { get; set; } = SizingModel.SliderPercentDefault;
    public double DefaultTitleFontSlider { get; set; } = SizingModel.SliderPercentDefault;

    public double FaceScaleFactor { get; set; } = 1.2;
    public int FaceMinNeighbors { get; set; } = 7;

    public bool AppendNumSuffixForOriginalSaves { get; set; } = true;
}

public static class AppSettingsStore
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AutoNum");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return CreateDefault();
            }

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefault();
            var originalSchemaVersion = settings.SchemaVersion;
            Migrate(settings);
            if (settings.SchemaVersion != originalSchemaVersion)
            {
                Save(settings);
            }
            return settings;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to load settings: {ex}");
            return CreateDefault();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            settings.SchemaVersion = 3;
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to save settings: {ex}");
        }
    }

    private static AppSettings CreateDefault() => new()
    {
        SchemaVersion = 3
    };

    private static void Migrate(AppSettings settings)
    {
        if (settings.SchemaVersion >= 3)
        {
            return;
        }

        settings.DefaultLabelDiameterSlider = (int)Math.Round(LegacySliderToPercent(settings.DefaultLabelDiameterSlider));
        settings.DefaultNamesFontSlider = LegacySliderToPercent(settings.DefaultNamesFontSlider);
        settings.DefaultTitleFontSlider = LegacySliderToPercent(settings.DefaultTitleFontSlider);
        settings.SchemaVersion = 3;
    }

    private static double LegacySliderToPercent(double legacySliderValue)
    {
        var clamped = Math.Clamp(legacySliderValue, 0, 100);
        var legacyScale = 0.5 + 0.0002 * clamped * clamped;
        return Math.Clamp(legacyScale * 100.0, SizingModel.SliderPercentMin, SizingModel.SliderPercentMax);
    }
}
