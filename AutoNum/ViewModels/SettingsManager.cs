using AutoNumber.Model;
using System.Drawing;

namespace AutoNumber.ViewModels;

/// <summary>
/// App-wide defaults, persisted to %AppData%/AutoNum/settings.json.
///
/// Persistence is intent-driven, never automatic: property setters only mutate in-memory
/// state, and <see cref="Save"/> is called explicitly — by the settings dialog's OK button
/// and by the format dialogs' "Als Standard übernehmen". The settings dialog edits the live
/// instance (so its "Anwenden" buttons see the edited values) inside a
/// <see cref="BeginEdit"/>/<see cref="CommitEdit"/>/<see cref="CancelEdit"/> transaction;
/// Cancel restores the snapshot taken at BeginEdit.
/// </summary>
public class SettingsManager : BaseViewModel
{
    private readonly AppSettings _settings;

    /// <summary>Per-element formatting defaults (see <see cref="ElementDefaults"/>).</summary>
    public ElementDefaults Labels { get; } = new();
    public ElementDefaults Title { get; } = new();
    public ElementDefaults ImageInfo { get; } = new();
    public ElementDefaults ImageId { get; } = new();
    public ElementDefaults Names { get; } = new();

    public SettingsManager()
    {
        _settings = AppSettingsStore.Load();

        Labels.Scale = _settings.DefaultLabelDiameterScale;
        Labels.FontColor = Color.FromArgb(_settings.DefaultLabelFontForeground);
        Labels.BackgroundColor = Color.FromArgb(_settings.DefaultLabelBackgroundColor);
        Labels.EdgeColor = Color.FromArgb(_settings.DefaultLabelEdgeColor);

        Title.Scale = _settings.DefaultTitleFontScale;
        Title.FontColor = Color.FromArgb(_settings.DefaultTitleFontForeground);
        Title.BackgroundColor = Color.FromArgb(_settings.DefaultTitleFontBackground);
        Title.Enabled = _settings.DefaultTitleEnabled;

        ImageInfo.Scale = _settings.DefaultImageInfoFontScale;
        ImageInfo.FontColor = Color.FromArgb(_settings.DefaultImageInfoFontForeground);
        ImageInfo.BackgroundColor = Color.FromArgb(_settings.DefaultImageInfoFontBackground);
        ImageInfo.Enabled = _settings.DefaultImageInfoEnabled;

        ImageId.Scale = _settings.DefaultImageIdFontScale;
        ImageId.FontColor = Color.FromArgb(_settings.DefaultImageIdFontForeground);
        ImageId.BackgroundColor = Color.FromArgb(_settings.DefaultImageIdFontBackground);
        ImageId.Enabled = _settings.DefaultImageIdEnabled;

        Names.Scale = _settings.DefaultNamesFontScale;
        Names.FontColor = Color.FromArgb(_settings.DefaultNamesFontForeground);
        Names.BackgroundColor = Color.FromArgb(_settings.DefaultNamesFontBackground);
        Names.Enabled = _settings.DefaultNamesEnabled;

        _faceDetectionEnabled = _settings.FaceDetectionEnabled;
        _rowDetectionEnabled = _settings.RowDetectionEnabled;
        _faceLabelAnchor = _settings.DefaultFaceLabelAnchor;
        _numberBottomUp = _settings.DefaultNumberBottomUp;
        _saveFileSuffix = _settings.SaveFileSuffix ?? "_num";
        _useCustomOutputFolder = _settings.UseCustomOutputFolder;
        _outputFolder = _settings.OutputFolder ?? string.Empty;
        _exportCsvMetadata = _settings.ExportCsvMetadata;
        _exportJsonMetadata = _settings.ExportJsonMetadata;
        _defaultSaveFormat = _settings.DefaultSaveFormat;
    }

    #region Scalar settings ------------------------------------------------

    public bool FaceDetectionEnabled
    {
        get => _faceDetectionEnabled;
        set
        {
            if (_faceDetectionEnabled == value)
            {
                return;
            }

            _faceDetectionEnabled = value;
            OnPropertyChanged(nameof(FaceDetectionEnabled));
            OnPropertyChanged(nameof(CanEnableRowDetection));

            // Row detection builds on face detection.
            if (!_faceDetectionEnabled)
            {
                RowDetectionEnabled = false;
            }
        }
    }

    public bool RowDetectionEnabled
    {
        get => _rowDetectionEnabled;
        set
        {
            var clamped = FaceDetectionEnabled && value;
            if (_rowDetectionEnabled != clamped)
            {
                _rowDetectionEnabled = clamped;
                OnPropertyChanged(nameof(RowDetectionEnabled));
            }
        }
    }

    public bool CanEnableRowDetection => FaceDetectionEnabled;

    /// <summary>
    /// Where a freshly detected face's label is centered, relative to the detected
    /// face rectangle. Applies only to new detections, never moves existing labels.
    /// </summary>
    public FaceLabelAnchor FaceLabelAnchor
    {
        get => _faceLabelAnchor;
        set => SetProperty(ref _faceLabelAnchor, value);
    }

    /// <summary>
    /// Default numbering direction for newly opened images: <c>false</c> numbers the top row
    /// first, <c>true</c> the bottom row. Seeds <see cref="LabelManager.NumberBottomUp"/>;
    /// per-image values restored from metadata are never overridden by it.
    /// </summary>
    public bool NumberBottomUp
    {
        get => _numberBottomUp;
        set => SetProperty(ref _numberBottomUp, value);
    }

    public string SaveFileSuffix
    {
        get => _saveFileSuffix;
        set => SetProperty(ref _saveFileSuffix, value ?? string.Empty);
    }

    public bool UseCustomOutputFolder
    {
        get => _useCustomOutputFolder;
        set => SetProperty(ref _useCustomOutputFolder, value);
    }

    public string OutputFolder
    {
        get => _outputFolder;
        set => SetProperty(ref _outputFolder, value ?? string.Empty);
    }

    public bool ExportCsvMetadata
    {
        get => _exportCsvMetadata;
        set => SetProperty(ref _exportCsvMetadata, value);
    }

    public bool ExportJsonMetadata
    {
        get => _exportJsonMetadata;
        set => SetProperty(ref _exportJsonMetadata, value);
    }

    /// <summary>
    /// Which format is preselected in the "Speichern unter..." dialog. The actual
    /// format used when saving is always decided by the extension the user picks.
    /// </summary>
    public SaveFormat DefaultSaveFormat
    {
        get => _defaultSaveFormat;
        set => SetProperty(ref _defaultSaveFormat, value);
    }

    #endregion

    #region Edit transaction (settings dialog) -----------------------------

    /// <summary>Captures the current state so <see cref="CancelEdit"/> can restore it.</summary>
    public void BeginEdit()
    {
        _editSnapshot = CaptureSnapshot();
    }

    /// <summary>Keeps the edited values and persists them.</summary>
    public void CommitEdit()
    {
        _editSnapshot = null;
        Save();
    }

    /// <summary>Restores the state captured by <see cref="BeginEdit"/>; nothing is written.</summary>
    public void CancelEdit()
    {
        if (_editSnapshot is not { } snapshot)
        {
            return;
        }

        _editSnapshot = null;
        Labels.Restore(snapshot.Labels);
        Title.Restore(snapshot.Title);
        ImageInfo.Restore(snapshot.ImageInfo);
        ImageId.Restore(snapshot.ImageId);
        Names.Restore(snapshot.Names);
        FaceDetectionEnabled = snapshot.FaceDetectionEnabled;
        RowDetectionEnabled = snapshot.RowDetectionEnabled;
        FaceLabelAnchor = snapshot.FaceLabelAnchor;
        NumberBottomUp = snapshot.NumberBottomUp;
        SaveFileSuffix = snapshot.SaveFileSuffix;
        UseCustomOutputFolder = snapshot.UseCustomOutputFolder;
        OutputFolder = snapshot.OutputFolder;
        ExportCsvMetadata = snapshot.ExportCsvMetadata;
        ExportJsonMetadata = snapshot.ExportJsonMetadata;
        DefaultSaveFormat = snapshot.DefaultSaveFormat;
    }

    private SettingsSnapshot CaptureSnapshot() => new(
        Labels.Capture(), Title.Capture(), ImageInfo.Capture(), ImageId.Capture(), Names.Capture(),
        FaceDetectionEnabled, RowDetectionEnabled, FaceLabelAnchor, NumberBottomUp,
        SaveFileSuffix, UseCustomOutputFolder, OutputFolder,
        ExportCsvMetadata, ExportJsonMetadata, DefaultSaveFormat);

    private sealed record SettingsSnapshot(
        ElementDefaults.Snapshot Labels,
        ElementDefaults.Snapshot Title,
        ElementDefaults.Snapshot ImageInfo,
        ElementDefaults.Snapshot ImageId,
        ElementDefaults.Snapshot Names,
        bool FaceDetectionEnabled,
        bool RowDetectionEnabled,
        FaceLabelAnchor FaceLabelAnchor,
        bool NumberBottomUp,
        string SaveFileSuffix,
        bool UseCustomOutputFolder,
        string OutputFolder,
        bool ExportCsvMetadata,
        bool ExportJsonMetadata,
        SaveFormat DefaultSaveFormat);

    #endregion

    /// <summary>
    /// Persists the current state to settings.json. The flat AppSettings JSON schema is kept
    /// unchanged so existing settings files stay valid.
    /// </summary>
    public void Save()
    {
        _settings.DefaultLabelDiameterScale = Labels.Scale;
        _settings.DefaultLabelFontForeground = Labels.FontColor.ToArgb();
        _settings.DefaultLabelBackgroundColor = Labels.BackgroundColor.ToArgb();
        _settings.DefaultLabelEdgeColor = Labels.EdgeColor.ToArgb();

        _settings.DefaultTitleFontScale = Title.Scale;
        _settings.DefaultTitleFontForeground = Title.FontColor.ToArgb();
        _settings.DefaultTitleFontBackground = Title.BackgroundColor.ToArgb();
        _settings.DefaultTitleEnabled = Title.Enabled;

        _settings.DefaultImageInfoFontScale = ImageInfo.Scale;
        _settings.DefaultImageInfoFontForeground = ImageInfo.FontColor.ToArgb();
        _settings.DefaultImageInfoFontBackground = ImageInfo.BackgroundColor.ToArgb();
        _settings.DefaultImageInfoEnabled = ImageInfo.Enabled;

        _settings.DefaultImageIdFontScale = ImageId.Scale;
        _settings.DefaultImageIdFontForeground = ImageId.FontColor.ToArgb();
        _settings.DefaultImageIdFontBackground = ImageId.BackgroundColor.ToArgb();
        _settings.DefaultImageIdEnabled = ImageId.Enabled;

        _settings.DefaultNamesFontScale = Names.Scale;
        _settings.DefaultNamesFontForeground = Names.FontColor.ToArgb();
        _settings.DefaultNamesFontBackground = Names.BackgroundColor.ToArgb();
        _settings.DefaultNamesEnabled = Names.Enabled;

        _settings.FaceDetectionEnabled = FaceDetectionEnabled;
        _settings.RowDetectionEnabled = RowDetectionEnabled;
        _settings.DefaultFaceLabelAnchor = FaceLabelAnchor;
        _settings.DefaultNumberBottomUp = NumberBottomUp;
        _settings.SaveFileSuffix = SaveFileSuffix;
        _settings.UseCustomOutputFolder = UseCustomOutputFolder;
        _settings.OutputFolder = OutputFolder;
        _settings.ExportCsvMetadata = ExportCsvMetadata;
        _settings.ExportJsonMetadata = ExportJsonMetadata;
        _settings.DefaultSaveFormat = DefaultSaveFormat;

        AppSettingsStore.Save(_settings);
    }

    /// <summary>
    /// Update the full default formatting for one element ("Als Standard übernehmen" in the
    /// formatting dialogs). Callers persist explicitly via <see cref="Save"/>.
    /// </summary>
    public void UpdateDefaultFormatting(string managerType, double scale, Color fontColor, Color backgroundColor, Color? edgeColor = null)
    {
        if (ElementFor(managerType) is not ElementDefaults element)
        {
            return;
        }

        element.Scale = scale;
        element.FontColor = fontColor;
        element.BackgroundColor = backgroundColor;
        if (edgeColor is Color edge)
        {
            element.EdgeColor = edge;
        }
    }

    private ElementDefaults? ElementFor(string managerType) => managerType switch
    {
        nameof(LabelManager) => Labels,
        nameof(TitleManager) => Title,
        nameof(ImageInfoManager) => ImageInfo,
        nameof(ImageIdManager) => ImageId,
        nameof(NameManager) => Names,
        _ => null
    };

    public void ApplyFreshImageDefaults(LabelManager labelManager, NameManager nameManager, TitleManager titleManager, ImageInfoManager imageInfoManager, ImageIdManager imageIdManager)
    {
        ApplyCurrentImageFormattingDefaults(labelManager, nameManager, titleManager, imageInfoManager, imageIdManager);
        ApplyCurrentImageVisibilityDefaults(nameManager, titleManager, imageInfoManager, imageIdManager);

        titleManager.Title = string.Empty;
        imageInfoManager.ImageInfo = string.Empty;
        imageIdManager.ImageId = string.Empty;
    }

    public void ApplyCurrentImageFormattingDefaults(LabelManager labelManager, NameManager nameManager, TitleManager titleManager, ImageInfoManager imageInfoManager, ImageIdManager imageIdManager)
    {
        labelManager.LabelScale = Labels.Scale;
        labelManager.FontColor = Labels.FontColor;
        labelManager.BackgroundColor = Labels.BackgroundColor;
        labelManager.EdgeColor = Labels.EdgeColor;

        nameManager.FontScale = Names.Scale;
        nameManager.FontColor = Names.FontColor;
        nameManager.BackgroundColor = Names.BackgroundColor;

        titleManager.FontScale = Title.Scale;
        titleManager.FontColor = Title.FontColor;
        titleManager.BackgroundColor = Title.BackgroundColor;

        imageInfoManager.FontScale = ImageInfo.Scale;
        imageInfoManager.FontColor = ImageInfo.FontColor;
        imageInfoManager.BackgroundColor = ImageInfo.BackgroundColor;

        imageIdManager.FontScale = ImageId.Scale;
        imageIdManager.FontColor = ImageId.FontColor;
        imageIdManager.BackgroundColor = ImageId.BackgroundColor;
    }

    public void ApplyCurrentImageVisibilityDefaults(NameManager nameManager, TitleManager titleManager, ImageInfoManager imageInfoManager, ImageIdManager imageIdManager)
    {
        nameManager.IsEnabled = Names.Enabled;
        titleManager.IsEnabled = Title.Enabled;
        imageInfoManager.IsEnabled = ImageInfo.Enabled;
        imageIdManager.IsEnabled = ImageId.Enabled;
    }

    private SettingsSnapshot? _editSnapshot;
    private bool _faceDetectionEnabled = true;
    private bool _rowDetectionEnabled = true;
    private FaceLabelAnchor _faceLabelAnchor;
    private bool _numberBottomUp;
    private string _saveFileSuffix = "_num";
    private bool _useCustomOutputFolder;
    private string _outputFolder = string.Empty;
    private bool _exportCsvMetadata;
    private bool _exportJsonMetadata;
    private SaveFormat _defaultSaveFormat;
}
