using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace AutoNumber.Model;

public class AppSettings
{
    public int SchemaVersion { get; set; }
    public bool DefaultNamesEnabled { get; set; } = true;
    public bool DefaultTitleEnabled { get; set; } = true;
    public bool DefaultImageInfoEnabled { get; set; } = true;
    public bool DefaultImageIdEnabled { get; set; } = true;

    public double DefaultLabelDiameterScale { get; set; } = SizingModel.DefaultScale;
    public double DefaultNamesFontScale { get; set; } = SizingModel.DefaultScale;
    public double DefaultTitleFontScale { get; set; } = SizingModel.DefaultScale;
    public double DefaultImageInfoFontScale { get; set; } = SizingModel.DefaultScale;
    public double DefaultImageIdFontScale { get; set; } = SizingModel.DefaultScale;

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

        settings.SchemaVersion = 3;
    }
}
