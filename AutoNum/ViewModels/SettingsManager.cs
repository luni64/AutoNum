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
        _defaultLabelDiameterSlider = ClampInt(_settings.DefaultLabelDiameterSlider, (int)SizingModel.SliderPercentMin, (int)SizingModel.SliderPercentMax);
        _defaultNamesFontSlider = ClampDouble(_settings.DefaultNamesFontSlider, SizingModel.SliderPercentMin, SizingModel.SliderPercentMax);
        _defaultTitleFontSlider = ClampDouble(_settings.DefaultTitleFontSlider, SizingModel.SliderPercentMin, SizingModel.SliderPercentMax);
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

    public int DefaultLabelDiameterSlider
    {
        get => _defaultLabelDiameterSlider;
        set
        {
            var clamped = ClampInt(value, (int)SizingModel.SliderPercentMin, (int)SizingModel.SliderPercentMax);
            SetProperty(ref _defaultLabelDiameterSlider, clamped);
            SaveSettings();
        }
    }

    public double DefaultNamesFontSlider
    {
        get => _defaultNamesFontSlider;
        set
        {
            var clamped = ClampDouble(value, SizingModel.SliderPercentMin, SizingModel.SliderPercentMax);
            SetProperty(ref _defaultNamesFontSlider, clamped);
            SaveSettings();
        }
    }

    public double DefaultTitleFontSlider
    {
        get => _defaultTitleFontSlider;
        set
        {
            var clamped = ClampDouble(value, SizingModel.SliderPercentMin, SizingModel.SliderPercentMax);
            SetProperty(ref _defaultTitleFontSlider, clamped);
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

    public void ApplyDetectionDefaults()
    {
        FaceDetector.ScaleFactor = FaceScaleFactor;
        FaceDetector.MinNeighbors = FaceMinNeighbors;
    }

    public void ApplyFreshImageDefaults(LabelManager labelManager, NameManager nameManager, TitleManager titleManager)
    {
        labelManager.Diameter = DefaultLabelDiameterSlider;

        nameManager.FontSizeSliderValue = DefaultNamesFontSlider;
        nameManager.IsEnabled = DefaultNamesEnabled;

        titleManager.FontSizeSliderValue = DefaultTitleFontSlider;
        titleManager.IsEnabled = DefaultTitleEnabled;
    }

    private void SaveSettings()
    {
        _settings.DefaultNamesEnabled = DefaultNamesEnabled;
        _settings.DefaultTitleEnabled = DefaultTitleEnabled;
        _settings.DefaultLabelDiameterSlider = DefaultLabelDiameterSlider;
        _settings.DefaultNamesFontSlider = DefaultNamesFontSlider;
        _settings.DefaultTitleFontSlider = DefaultTitleFontSlider;
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
    private int _defaultLabelDiameterSlider;
    private double _defaultNamesFontSlider;
    private double _defaultTitleFontSlider;
    private double _faceScaleFactor;
    private int _faceMinNeighbors;
    private bool _appendNumSuffixForOriginalSaves;
}
