using AutoNumber.Model;
using System.Drawing;

namespace AutoNumber.ViewModels;

public class SettingsManager : BaseViewModel
{
    private readonly AppSettings _settings;

    public SettingsManager()
    {
        _settings = AppSettingsStore.Load();

        _defaultNamesEnabled = _settings.DefaultNamesEnabled;
        _defaultTitleEnabled = _settings.DefaultTitleEnabled;
        _defaultImageInfoEnabled = _settings.DefaultImageInfoEnabled;
        _defaultImageIdEnabled = _settings.DefaultImageIdEnabled;

        _defaultLabelDiameterScale = ClampDouble(_settings.DefaultLabelDiameterScale, 0.25, 4.0);
        _defaultNamesFontScale = ClampDouble(_settings.DefaultNamesFontScale, 0.25, 4.0);
        _defaultTitleFontScale = ClampDouble(_settings.DefaultTitleFontScale, 0.25, 4.0);
        _defaultImageInfoFontScale = ClampDouble(_settings.DefaultImageInfoFontScale, 0.25, 4.0);
        _defaultImageIdFontScale = ClampDouble(_settings.DefaultImageIdFontScale, 0.25, 4.0);

        _defaultLabelFontColor = Color.FromArgb(_settings.DefaultLabelFontForeground);
        _defaultLabelBackgroundColor = Color.FromArgb(_settings.DefaultLabelBackgroundColor);
        _defaultLabelEdgeColor = Color.FromArgb(_settings.DefaultLabelEdgeColor);
        _defaultNamesFontColor = Color.FromArgb(_settings.DefaultNamesFontForeground);
        _defaultNamesBackgroundColor = Color.FromArgb(_settings.DefaultNamesFontBackground);
        _defaultTitleFontColor = Color.FromArgb(_settings.DefaultTitleFontForeground);
        _defaultTitleBackgroundColor = Color.FromArgb(_settings.DefaultTitleFontBackground);
        _defaultImageInfoFontColor = Color.FromArgb(_settings.DefaultImageInfoFontForeground);
        _defaultImageInfoBackgroundColor = Color.FromArgb(_settings.DefaultImageInfoFontBackground);
        _defaultImageIdFontColor = Color.FromArgb(_settings.DefaultImageIdFontForeground);
        _defaultImageIdBackgroundColor = Color.FromArgb(_settings.DefaultImageIdFontBackground);

        _faceScaleFactor = ClampDouble(_settings.FaceScaleFactor, 1.05, 2.0);
        _faceMinNeighbors = ClampInt(_settings.FaceMinNeighbors, 1, 20);

        _exportCsvMetadata = _settings.ExportCsvMetadata;
        _exportJsonMetadata = _settings.ExportJsonMetadata;
        _saveFileSuffix = _settings.SaveFileSuffix ?? "_num";

        ApplyDetectionDefaults();
    }


    public bool DefaultNamesEnabled
    {
        get => _defaultNamesEnabled;
        set
        {
            SetProperty(ref _defaultNamesEnabled, value);
            SaveSettings();
        }
    }

    public bool DefaultTitleEnabled
    {
        get => _defaultTitleEnabled;
        set
        {
            SetProperty(ref _defaultTitleEnabled, value);
            SaveSettings();
        }
    }

    public bool DefaultImageInfoEnabled
    {
        get => _defaultImageInfoEnabled;
        set
        {
            SetProperty(ref _defaultImageInfoEnabled, value);
            SaveSettings();
        }
    }

    public bool DefaultImageIdEnabled
    {
        get => _defaultImageIdEnabled;
        set
        {
            SetProperty(ref _defaultImageIdEnabled, value);
            SaveSettings();
        }
    }

    /// <summary>
    /// Default label scale (model property, 0.25–4.0).
    /// </summary>
    public double DefaultLabelDiameterScale
    {
        get => _defaultLabelDiameterScale;
        set
        {
            var clamped = ClampDouble(value, 0.25, 4.0);
            SetProperty(ref _defaultLabelDiameterScale, clamped);
            SaveSettings();
        }
    }

    public Color DefaultLabelFontColor
    {
        get => _defaultLabelFontColor;
        set
        {
            SetProperty(ref _defaultLabelFontColor, value);
            SaveSettings();
        }
    }

    public Color DefaultLabelBackgroundColor
    {
        get => _defaultLabelBackgroundColor;
        set
        {
            SetProperty(ref _defaultLabelBackgroundColor, value);
            SaveSettings();
        }
    }

    public Color DefaultLabelEdgeColor
    {
        get => _defaultLabelEdgeColor;
        set
        {
            SetProperty(ref _defaultLabelEdgeColor, value);
            SaveSettings();
        }
    }

    /// <summary>
    /// Default names font scale (model property, 0.25–4.0).
    /// </summary>
    public double DefaultNamesFontScale
    {
        get => _defaultNamesFontScale;
        set
        {
            var clamped = ClampDouble(value, 0.25, 4.0);
            SetProperty(ref _defaultNamesFontScale, clamped);
            SaveSettings();
        }
    }

    /// <summary>
    /// Default title font scale (model property, 0.25–4.0).
    /// </summary>
    public double DefaultTitleFontScale
    {
        get => _defaultTitleFontScale;
        set
        {
            var clamped = ClampDouble(value, 0.25, 4.0);
            SetProperty(ref _defaultTitleFontScale, clamped);
            SaveSettings();
        }
    }

    /// <summary>
    /// Default image-info font scale (model property, 0.25–4.0).
    /// </summary>
    public double DefaultImageInfoFontScale
    {
        get => _defaultImageInfoFontScale;
        set
        {
            var clamped = ClampDouble(value, 0.25, 4.0);
            SetProperty(ref _defaultImageInfoFontScale, clamped);
            SaveSettings();
        }
    }

    /// <summary>
    /// Default image-ID font scale (model property, 0.25–4.0).
    /// </summary>
    public double DefaultImageIdFontScale
    {
        get => _defaultImageIdFontScale;
        set
        {
            var clamped = ClampDouble(value, 0.25, 4.0);
            SetProperty(ref _defaultImageIdFontScale, clamped);
            SaveSettings();
        }
    }

    public Color DefaultNamesFontColor
    {
        get => _defaultNamesFontColor;
        set
        {
            SetProperty(ref _defaultNamesFontColor, value);
            SaveSettings();
        }
    }

    public Color DefaultNamesBackgroundColor
    {
        get => _defaultNamesBackgroundColor;
        set
        {
            SetProperty(ref _defaultNamesBackgroundColor, value);
            SaveSettings();
        }
    }

    public Color DefaultTitleFontColor
    {
        get => _defaultTitleFontColor;
        set
        {
            SetProperty(ref _defaultTitleFontColor, value);
            SaveSettings();
        }
    }

    public Color DefaultTitleBackgroundColor
    {
        get => _defaultTitleBackgroundColor;
        set
        {
            SetProperty(ref _defaultTitleBackgroundColor, value);
            SaveSettings();
        }
    }

    public Color DefaultImageInfoFontColor
    {
        get => _defaultImageInfoFontColor;
        set
        {
            SetProperty(ref _defaultImageInfoFontColor, value);
            SaveSettings();
        }
    }

    public Color DefaultImageInfoBackgroundColor
    {
        get => _defaultImageInfoBackgroundColor;
        set
        {
            SetProperty(ref _defaultImageInfoBackgroundColor, value);
            SaveSettings();
        }
    }

    public Color DefaultImageIdFontColor
    {
        get => _defaultImageIdFontColor;
        set
        {
            SetProperty(ref _defaultImageIdFontColor, value);
            SaveSettings();
        }
    }

    public Color DefaultImageIdBackgroundColor
    {
        get => _defaultImageIdBackgroundColor;
        set
        {
            SetProperty(ref _defaultImageIdBackgroundColor, value);
            SaveSettings();
        }
    }

    public double FaceScaleFactor
    {
        get => _faceScaleFactor;
        set
        {
            var clamped = ClampDouble(value, 1.05, 2.0);
            SetProperty(ref _faceScaleFactor, clamped);
            FaceDetector.ScaleFactor = clamped;
            SaveSettings();
        }
    }

    public int FaceMinNeighbors
    {
        get => _faceMinNeighbors;
        set
        {
            var clamped = ClampInt(value, 1, 20);
            SetProperty(ref _faceMinNeighbors, clamped);
            FaceDetector.MinNeighbors = clamped;
            SaveSettings();
        }
    }

    public bool AppendNumSuffixForOriginalSaves
    {
        get => !string.IsNullOrWhiteSpace(_saveFileSuffix);
        set
        {
            if (value && string.IsNullOrWhiteSpace(_saveFileSuffix))
            {
                SaveFileSuffix = "_num";
            }
            else if (!value)
            {
                SaveFileSuffix = string.Empty;
            }
        }
    }

    public string SaveFileSuffix
    {
        get => _saveFileSuffix;
        set
        {
            SetProperty(ref _saveFileSuffix, value ?? string.Empty);
            SaveSettings();
        }
    }

    public bool ExportCsvMetadata
    {
        get => _exportCsvMetadata;
        set
        {
            SetProperty(ref _exportCsvMetadata, value);
            OnPropertyChanged(nameof(CanExportNow));
            SaveSettings();
        }
    }

    public bool ExportJsonMetadata
    {
        get => _exportJsonMetadata;
        set
        {
            SetProperty(ref _exportJsonMetadata, value);
            OnPropertyChanged(nameof(CanExportNow));
            SaveSettings();
        }
    }

    public bool CanExportNow => ExportCsvMetadata || ExportJsonMetadata;

    private RelayCommand? _readCurrentValuesCommand;
    public RelayCommand ReadCurrentValuesCommand => _readCurrentValuesCommand ??= new RelayCommand(ExecuteReadCurrentValues);

    private void ExecuteReadCurrentValues(object? obj)
    {
        if (obj is not MainVM mainVM)
        {
            return;
        }

        // Read current formatting values from managers
        DefaultLabelDiameterScale = mainVM.LabelManager.LabelScale;
        DefaultLabelFontColor = mainVM.LabelManager.FontColor;
        DefaultLabelBackgroundColor = mainVM.LabelManager.BackgroundColor;
        DefaultLabelEdgeColor = mainVM.LabelManager.EdgeColor;

        DefaultNamesFontScale = mainVM.NameManager.FontScale;
        DefaultNamesFontColor = mainVM.NameManager.FontColor;
        DefaultNamesBackgroundColor = mainVM.NameManager.BackgroundColor;

        DefaultTitleFontScale = mainVM.TitleManager.FontScale;
        DefaultTitleFontColor = mainVM.TitleManager.TitleFontColor;
        DefaultTitleBackgroundColor = mainVM.TitleManager.BackgroundColor;

        DefaultImageInfoFontScale = mainVM.ImageInfoManager.FontScale;
        DefaultImageInfoFontColor = mainVM.ImageInfoManager.ImageInfoFontColor;
        DefaultImageInfoBackgroundColor = mainVM.ImageInfoManager.BackgroundColor;

        DefaultImageIdFontScale = mainVM.ImageIdManager.FontScale;
        DefaultImageIdFontColor = mainVM.ImageIdManager.FontColor;
        DefaultImageIdBackgroundColor = mainVM.ImageIdManager.BackgroundColor;

        // Save the settings
        SaveSettings();
    }

    /// <summary>
    /// Update a specific default scale value (for "Use as default" buttons in formatting dialogs).
    /// </summary>
    public void UpdateDefaultScale(string managerType, double scale)
    {
        scale = ClampDouble(scale, 0.25, 4.0);

        switch (managerType)
        {
            case nameof(TitleManager):
                DefaultTitleFontScale = scale;
                break;
            case nameof(ImageInfoManager):
                DefaultImageInfoFontScale = scale;
                break;
            case nameof(ImageIdManager):
                DefaultImageIdFontScale = scale;
                break;
            case nameof(NameManager):
                DefaultNamesFontScale = scale;
                break;
        }
    }

    /// <summary>
    /// Update the full default formatting value for one element.
    /// </summary>
    public void UpdateDefaultFormatting(string managerType, double scale, Color fontColor, Color backgroundColor)
    {
        scale = ClampDouble(scale, 0.25, 4.0);

        switch (managerType)
        {
            case nameof(TitleManager):
                DefaultTitleFontScale = scale;
                DefaultTitleFontColor = fontColor;
                DefaultTitleBackgroundColor = backgroundColor;
                break;
            case nameof(ImageInfoManager):
                DefaultImageInfoFontScale = scale;
                DefaultImageInfoFontColor = fontColor;
                DefaultImageInfoBackgroundColor = backgroundColor;
                break;
            case nameof(ImageIdManager):
                DefaultImageIdFontScale = scale;
                DefaultImageIdFontColor = fontColor;
                DefaultImageIdBackgroundColor = backgroundColor;
                break;
            case nameof(NameManager):
                DefaultNamesFontScale = scale;
                DefaultNamesFontColor = fontColor;
                DefaultNamesBackgroundColor = backgroundColor;
                break;
        }
    }

    public void ApplyDetectionDefaults()
    {
        FaceDetector.ScaleFactor = FaceScaleFactor;
        FaceDetector.MinNeighbors = FaceMinNeighbors;
    }

    public void ApplyFreshImageDefaults(LabelManager labelManager, NameManager nameManager, TitleManager titleManager, ImageInfoManager imageInfoManager, ImageIdManager imageIdManager)
    {
        // Fresh images start with stored label defaults, and other elements use their saved defaults
        labelManager.LabelScale = DefaultLabelDiameterScale;
        labelManager.FontColor = DefaultLabelFontColor;
        labelManager.BackgroundColor = DefaultLabelBackgroundColor;
        labelManager.EdgeColor = DefaultLabelEdgeColor;

        nameManager.FontScale = DefaultNamesFontScale;
        nameManager.FontColor = DefaultNamesFontColor;
        nameManager.BackgroundColor = DefaultNamesBackgroundColor;
        nameManager.IsEnabled = DefaultNamesEnabled;

        titleManager.FontScale = DefaultTitleFontScale;
        titleManager.TitleFontColor = DefaultTitleFontColor;
        titleManager.BackgroundColor = DefaultTitleBackgroundColor;
        titleManager.Title = string.Empty;
        titleManager.IsEnabled = DefaultTitleEnabled;

        imageInfoManager.FontScale = DefaultImageInfoFontScale;
        imageInfoManager.ImageInfoFontColor = DefaultImageInfoFontColor;
        imageInfoManager.BackgroundColor = DefaultImageInfoBackgroundColor;
        imageInfoManager.ImageInfo = string.Empty;
        imageInfoManager.IsEnabled = DefaultImageInfoEnabled;

        imageIdManager.FontScale = DefaultImageIdFontScale;
        imageIdManager.FontColor = DefaultImageIdFontColor;
        imageIdManager.BackgroundColor = DefaultImageIdBackgroundColor;
        imageIdManager.ImageId = string.Empty;
        imageIdManager.IsEnabled = DefaultImageIdEnabled;
    }

    public void ApplyCurrentImageFormattingDefaults(LabelManager labelManager, NameManager nameManager, TitleManager titleManager, ImageInfoManager imageInfoManager, ImageIdManager imageIdManager)
    {
        labelManager.LabelScale = DefaultLabelDiameterScale;
        labelManager.FontColor = DefaultLabelFontColor;
        labelManager.BackgroundColor = DefaultLabelBackgroundColor;
        labelManager.EdgeColor = DefaultLabelEdgeColor;

        nameManager.FontScale = DefaultNamesFontScale;
        nameManager.FontColor = DefaultNamesFontColor;
        nameManager.BackgroundColor = DefaultNamesBackgroundColor;

        titleManager.FontScale = DefaultTitleFontScale;
        titleManager.TitleFontColor = DefaultTitleFontColor;
        titleManager.BackgroundColor = DefaultTitleBackgroundColor;

        imageInfoManager.FontScale = DefaultImageInfoFontScale;
        imageInfoManager.ImageInfoFontColor = DefaultImageInfoFontColor;
        imageInfoManager.BackgroundColor = DefaultImageInfoBackgroundColor;

        imageIdManager.FontScale = DefaultImageIdFontScale;
        imageIdManager.FontColor = DefaultImageIdFontColor;
        imageIdManager.BackgroundColor = DefaultImageIdBackgroundColor;
    }

    public void ApplyCurrentImageVisibilityDefaults(NameManager nameManager, TitleManager titleManager, ImageInfoManager imageInfoManager, ImageIdManager imageIdManager)
    {
        nameManager.IsEnabled = DefaultNamesEnabled;
        titleManager.IsEnabled = DefaultTitleEnabled;
        imageInfoManager.IsEnabled = DefaultImageInfoEnabled;
        imageIdManager.IsEnabled = DefaultImageIdEnabled;
    }

    private void SaveSettings()
    {
        _settings.DefaultNamesEnabled = DefaultNamesEnabled;
        _settings.DefaultTitleEnabled = DefaultTitleEnabled;
        _settings.DefaultImageInfoEnabled = DefaultImageInfoEnabled;
        _settings.DefaultImageIdEnabled = DefaultImageIdEnabled;
        _settings.DefaultLabelDiameterScale = DefaultLabelDiameterScale;
        _settings.DefaultNamesFontScale = DefaultNamesFontScale;
        _settings.DefaultTitleFontScale = DefaultTitleFontScale;
        _settings.DefaultImageInfoFontScale = DefaultImageInfoFontScale;
        _settings.DefaultImageIdFontScale = DefaultImageIdFontScale;
        _settings.DefaultLabelFontForeground = DefaultLabelFontColor.ToArgb();
        _settings.DefaultLabelBackgroundColor = DefaultLabelBackgroundColor.ToArgb();
        _settings.DefaultLabelEdgeColor = DefaultLabelEdgeColor.ToArgb();
        _settings.DefaultNamesFontForeground = DefaultNamesFontColor.ToArgb();
        _settings.DefaultNamesFontBackground = DefaultNamesBackgroundColor.ToArgb();
        _settings.DefaultTitleFontForeground = DefaultTitleFontColor.ToArgb();
        _settings.DefaultTitleFontBackground = DefaultTitleBackgroundColor.ToArgb();
        _settings.DefaultImageInfoFontForeground = DefaultImageInfoFontColor.ToArgb();
        _settings.DefaultImageInfoFontBackground = DefaultImageInfoBackgroundColor.ToArgb();
        _settings.DefaultImageIdFontForeground = DefaultImageIdFontColor.ToArgb();
        _settings.DefaultImageIdFontBackground = DefaultImageIdBackgroundColor.ToArgb();
        _settings.FaceScaleFactor = FaceScaleFactor;
        _settings.FaceMinNeighbors = FaceMinNeighbors;
        _settings.SaveFileSuffix = SaveFileSuffix;
        _settings.ExportCsvMetadata = ExportCsvMetadata;
        _settings.ExportJsonMetadata = ExportJsonMetadata;
        AppSettingsStore.Save(_settings);
    }

    private static int ClampInt(int value, int min, int max) => Math.Min(max, Math.Max(min, value));

    private static double ClampDouble(double value, double min, double max)
    {
        if (!double.IsFinite(value))
        {
            return min;
        }

        return Math.Min(max, Math.Max(min, value));
    }

    private bool _defaultNamesEnabled;
    private bool _defaultTitleEnabled;
    private bool _defaultImageInfoEnabled;
    private bool _defaultImageIdEnabled;
    private double _defaultLabelDiameterScale;
    private double _defaultNamesFontScale;
    private double _defaultTitleFontScale;
    private double _defaultImageInfoFontScale;
    private double _defaultImageIdFontScale;
    private Color _defaultLabelFontColor;
    private Color _defaultLabelBackgroundColor;
    private Color _defaultLabelEdgeColor;
    private Color _defaultNamesFontColor;
    private Color _defaultNamesBackgroundColor;
    private Color _defaultTitleFontColor;
    private Color _defaultTitleBackgroundColor;
    private Color _defaultImageInfoFontColor;
    private Color _defaultImageInfoBackgroundColor;
    private Color _defaultImageIdFontColor;
    private Color _defaultImageIdBackgroundColor;
    private double _faceScaleFactor;
    private int _faceMinNeighbors;
    private string _saveFileSuffix = "_num";
    private bool _exportCsvMetadata;
    private bool _exportJsonMetadata;
}
