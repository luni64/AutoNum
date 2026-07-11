using AutoNumber.Infrastructure;
using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;
using System.Drawing;

namespace AutoNumber.ViewModels;

/// <summary>
/// Shared behavior of the single-text elements — title, description (image info) and image-ID:
/// scale-factor font sizing against <see cref="LabelManager.BaseTextFontSize"/>, colors and
/// font family, visibility, and the restore-from-metadata flow. Derived classes supply the
/// element-specific metadata accessors and their own text/state on top.
///
/// <see cref="NameManager"/> deliberately does NOT derive from this: the names list pushes its
/// styling into the shared <see cref="TextLabel.Style"/> and owns table layout (PersonsView,
/// ShowNames batching), which is a different shape of manager.
/// </summary>
public abstract class TextElementManagerBase : BaseViewModel
{
    protected TextElementManagerBase(LabelManager labelManager)
    {
        LabelManager = labelManager;

        WeakReferenceMessenger.Default.Register<LabelsChangedMessage>(this, (r, msg) =>
        {
            ((TextElementManagerBase)r).ApplyScale();
        });

        WeakReferenceMessenger.Default.Register<MetadataLoadedMessage>(this, (r, msg) =>
        {
            ((TextElementManagerBase)r).RestoreFromMetadata(msg.Metadata);
        });
    }

    /// <summary>
    /// Font scale factor (0.25–4.0). Model property that drives the actual font size.
    /// </summary>
    public double FontScale
    {
        get => _fontScale;
        set
        {
            var clamped = Math.Clamp(value, 0.25, 4.0);
            if (_fontScale != clamped)
            {
                _fontScale = clamped;
                ApplyScale();
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Displayed size = BaseTextFontSize * FontScale; recomputed by <see cref="ApplyScale"/>.</summary>
    public double FontSize
    {
        get => _fontSize;
        private set => SetProperty(ref _fontSize, value);
    }

    public Color FontColor
    {
        get => _fontColor;
        set => SetProperty(ref _fontColor, value);
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value);
    }

    public FontFamily FontFamily
    {
        get => _fontFamily;
        set
        {
            if (_fontFamily == value)
            {
                return;
            }

            SetProperty(ref _fontFamily, value);
            OnAppearanceChanged();
        }
    }

    public virtual bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    protected LabelManager LabelManager { get; }

    protected void ApplyScale()
    {
        var baseTextFontSize = LabelManager.BaseTextFontSize;
        if (baseTextFontSize <= 0)
        {
            return;
        }

        FontSize = SizingModel.ResolveSize(baseTextFontSize, FontScale);
        OnAppearanceChanged();
    }

    /// <summary>
    /// Called after the font size or family changed; for derived state that depends on the
    /// rendered text metrics (e.g. the image-ID line height).
    /// </summary>
    protected virtual void OnAppearanceChanged()
    {
    }

    private void RestoreFromMetadata(AutoNumMetaData_V1 md)
    {
        try
        {
            Trace.WriteLine($"MetadataLoaded[{GetType().Name}]: start version={md.Version}");

            var font = MetadataFont(md);
            BackgroundColor = Color.FromArgb(font.Background);
            FontColor = Color.FromArgb(font.Foreground);
            FontFamily = FontFamilyResolver.Resolve(font.Family, FontFamily);

            FontScale = md is AutoNumMetaData_V3 v3
                ? MetadataScale(v3)
                : SizingModel.SafeScale(LegacyStoredFontSize(md), md.LabelsFont.Size);

            RestoreElementState(md);

            Trace.WriteLine($"MetadataLoaded[{GetType().Name}]: scale={FontScale:F4}, resolvedFontSize={FontSize:F4}, enabled={IsEnabled}");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"MetadataLoaded[{GetType().Name}]: failed - {ex}");
            throw;
        }
    }

    /// <summary>This element's font entry in the metadata.</summary>
    protected abstract AutoNumFont MetadataFont(AutoNumMetaData_V1 md);

    /// <summary>This element's exact scale stored in V3+ metadata.</summary>
    protected abstract double MetadataScale(AutoNumMetaData_V3 v3);

    /// <summary>
    /// Stored absolute font size used to derive a scale from V1/V2 metadata (relative to the
    /// legacy label font size).
    /// </summary>
    protected virtual double LegacyStoredFontSize(AutoNumMetaData_V1 md) => MetadataFont(md).Size;

    /// <summary>Restores the element-specific state (text, enabled flag) from metadata.</summary>
    protected abstract void RestoreElementState(AutoNumMetaData_V1 md);

    private double _fontScale = 1.0;
    private double _fontSize = 1;
    private Color _fontColor = Color.Black;
    private Color _backgroundColor = Color.White;
    private FontFamily _fontFamily = new("Calibri");
    private bool _isEnabled;
}
