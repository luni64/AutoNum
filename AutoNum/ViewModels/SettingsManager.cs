using AutoNumber.Model;

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

        _faceScaleFactor = ClampDouble(_settings.FaceScaleFactor, 1.05, 2.0);
        _faceMinNeighbors = ClampInt(_settings.FaceMinNeighbors, 1, 20);
        _appendNumSuffixForOriginalSaves = _settings.AppendNumSuffixForOriginalSaves;

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
        get => _appendNumSuffixForOriginalSaves;
        set
        {
            SetProperty(ref _appendNumSuffixForOriginalSaves, value);
            SaveSettings();
        }
    }

    private RelayCommand? _readCurrentValuesCommand;
    public RelayCommand ReadCurrentValuesCommand => _readCurrentValuesCommand ??= new RelayCommand(ExecuteReadCurrentValues);

    private void ExecuteReadCurrentValues(object? obj)
    {
        if (obj is not MainVM mainVM)
        {
            return;
        }

        // Read current scale values from managers
        DefaultNamesFontScale = mainVM.NameManager.FontScale;
        DefaultTitleFontScale = mainVM.TitleManager.FontScale;
        DefaultImageInfoFontScale = mainVM.ImageInfoManager.FontScale;
        DefaultImageIdFontScale = mainVM.ImageIdManager.FontScale;

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

    public void ApplyDetectionDefaults()
    {
        FaceDetector.ScaleFactor = FaceScaleFactor;
        FaceDetector.MinNeighbors = FaceMinNeighbors;
    }

    public void ApplyFreshImageDefaults(LabelManager labelManager, NameManager nameManager, TitleManager titleManager, ImageInfoManager imageInfoManager, ImageIdManager imageIdManager)
    {
        // Fresh images start unscaled, except for names, title, image-info, image-ID which use saved defaults
        labelManager.LabelScale = 1.0; // Labels always start unscaled
        nameManager.FontScale = DefaultNamesFontScale;
        nameManager.IsEnabled = DefaultNamesEnabled;
        titleManager.FontScale = DefaultTitleFontScale;
        titleManager.IsEnabled = DefaultTitleEnabled;
        imageInfoManager.FontScale = DefaultImageInfoFontScale;
        imageInfoManager.IsEnabled = DefaultImageInfoEnabled;
        imageIdManager.FontScale = DefaultImageIdFontScale;
        imageIdManager.IsEnabled = DefaultImageIdEnabled;
    }

    public void ApplyCurrentImageFontDefaults(NameManager nameManager, TitleManager titleManager, ImageInfoManager imageInfoManager, ImageIdManager imageIdManager)
    {
        nameManager.FontScale = DefaultNamesFontScale;
        titleManager.FontScale = DefaultTitleFontScale;
        imageInfoManager.FontScale = DefaultImageInfoFontScale;
        imageIdManager.FontScale = DefaultImageIdFontScale;
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
        _settings.FaceScaleFactor = FaceScaleFactor;
        _settings.FaceMinNeighbors = FaceMinNeighbors;
        _settings.AppendNumSuffixForOriginalSaves = AppendNumSuffixForOriginalSaves;
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
    private double _faceScaleFactor;
    private int _faceMinNeighbors;
    private bool _appendNumSuffixForOriginalSaves;
}
