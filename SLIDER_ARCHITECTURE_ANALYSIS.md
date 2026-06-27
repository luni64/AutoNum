# Slider Architecture Analysis - AutoNum Font Size Control

## Summary
The slider system is designed to control font sizes across multiple text elements (labels, names, title, image info, image ID). The connection between slider and displayed elements is broken because the UI components (FontManager) is not properly bound to the ViewModel properties.

## Architecture Overview

### Three-Layer Model

```
VIEW (FontManager Slider)
	↓ (0-1 range)
CONVERTER (SliderToScaleConverter)
	↓ (0.25-4.0 range)
MODEL (ManagerVM properties like Diameter, FontScale)
	↓ (applies to shared style objects)
STYLE OBJECTS (LabelStyle, TextStyle)
	↓ (used by rendering)
VISUAL ELEMENTS (Labels, Names, etc.)
```

### 1. VIEW LAYER: FontManager Control
**File**: `Views/FontManager.xaml` & `Views/FontManager.xaml.cs`

- Contains a Slider (Range: 0-1) for UI positioning
- Two color pickers (FontColor, BackgroundColor)
- **Dependency Properties**:
  - `SelectedFontSize` (0-1) - the slider's internal position
  - `FontColor` (Color)
  - `BackgroundColor` (Color)
- The Slider itself (`FontSizeSlider`) is the child control that needs binding

**Issue**: The FontManager.xaml.cs defines `SelectedFontSize` property, but the Slider in XAML doesn't have a binding!

```xaml
<!-- Missing binding! Slider is completely disconnected -->
<Slider x:Name="FontSizeSlider" Grid.Column="1"
	Minimum="0" Maximum="1"
	IsSnapToTickEnabled="False"
	mah:SliderHelper.EnableMouseWheel="MouseHover" />
```

### 2. CONVERTER LAYER: SliderToScaleConverter
**File**: `Infrastructure/Converters.cs`

Handles bidirectional conversion:
- **Convert** (Model → View): `scale (0.25-4.0)` → `sliderPosition (0-1)`
- **ConvertBack** (View → Model): `sliderPosition (0-1)` → `scale (0.25-4.0)`

Uses **quadratic mapping** via `SizingModel.SliderToScale()` and `SizingModel.ScaleToSlider()`:
```
scale = 4.5x² - 0.75x + 0.25
Maps:
  - slider 0.0 (left)   → scale 0.25
  - slider 0.5 (center) → scale 1.0
  - slider 1.0 (right)  → scale 4.0
```

### 3. MODEL LAYER: ViewModel Properties
**Files**: 
- `ViewModels/LabelManager.cs` - `Diameter` property (0.25-4.0)
- `ViewModels/NameManager.cs` - `FontScale` property (0.25-4.0)
- `ViewModels/TitleManager.cs` - `FontScale` property (0.25-4.0)
- `ViewModels/ImageInfoManager.cs` - `FontScale` property (0.25-4.0)
- `ViewModels/ImageIdManager.cs` - `FontScale` property (0.25-4.0)

Each manager has:
```csharp
public double FontScale
{
	get => _fontScale;
	set
	{
		var clamped = Math.Clamp(value, 0.25, 4.0);
		if (_fontScale != clamped)
		{
			_fontScale = clamped;
			ApplyScale();  // <-- Triggers style update
			OnPropertyChanged();
		}
	}
}

private void ApplyScale()
{
	// Example from NameManager
	TextLabel.Style.FontSize = SizingModel.ResolveSize(baseLabelFontSize, FontScale);
}
```

### 4. STYLE OBJECTS LAYER: Shared Visual State
**Files**:
- `ViewModels/LabelStyle.cs` - Controls label diameter and font size
- `ViewModels/TextStyle.cs` - Controls text (names) font size

Static instances accessed by all rendering components:
```csharp
public class LabelStyle : BaseViewModel
{
	public float Diameter { get; set; }      // Scales label circle
	public double FontSize { get; set; }     // Scales numbers in labels
}

public class TextStyle : BaseViewModel
{
	public double FontSize { get; set; }     // Scales name/title text
}
```

Referenced in rendering:
- `MarkerLabel.Style` (static in LabelManager)
- `TextLabel.Style` (static in NameManager, TitleManager, etc.)

### 5. VISUAL RENDERING LAYER
These style properties are bound in XAML templates for actual display.

---

## Current Connection Points

### Location 1: Main Window Slider (LabelWiz.xaml)
**Works Correctly** ✓

In `Views/WizardViews/LabelWiz.xaml`:
```xaml
<view:FontManager
	Grid.Column="0"
	SelectedFontSize="{Binding Diameter}"
	FontColor="{Binding FontColor, Mode=TwoWay, Converter={StaticResource ColorConverter}}"
	BackgroundColor="{Binding BackgroundColor, Mode=TwoWay, Converter={StaticResource ColorConverter}}"
/>
```

- Binds `SelectedFontSize` property to `LabelManager.Diameter`
- **Problem**: `SelectedFontSize` property exists but the Slider inside FontManager is not bound to it!

### Location 2: TextFormatDialog (Formatting Windows)
**Broken** ✗

In `Views/TextFormatDialog.xaml.cs`:
```csharp
private void BindFontManager(string fontColorPath, string backgroundColorPath, string fontSizePath)
{
	// Correctly binds colors
	BindingOperations.SetBinding(FontManagerControl, FontManager.FontColorProperty, ...);
	BindingOperations.SetBinding(FontManagerControl, FontManager.BackgroundColorProperty, ...);

	// Tries to bind slider directly
	var sliderBinding = new Binding(fontSizePath) 
	{ 
		Mode = BindingMode.TwoWay, 
		Converter = SliderToScaleConverter 
	};

	BindingOperations.SetBinding(
		FontManagerControl.FontSizeSlider,  // <-- Direct binding to Slider
		Slider.ValueProperty,
		sliderBinding);
}
```

This binding **bypasses** the `SelectedFontSize` property entirely!

---

## The Broken Connection

### Root Cause
The Slider control in FontManager.xaml has **no binding at all**:

```xaml
<Slider x:Name="FontSizeSlider" Grid.Column="1"
	Margin="0,0,20,0"
	Focusable="False"
	HorizontalAlignment="Stretch"
	Minimum="0"
	Maximum="1"
	IsSnapToTickEnabled="False"
	mah:SliderHelper.EnableMouseWheel="MouseHover" />
	<!-- ^^ NO Value BINDING! -->
```

### Why It's Broken

1. **In LabelWiz.xaml**: 
   - `FontManager.SelectedFontSize` property is bound to `LabelManager.Diameter` ✓
   - But the **Slider itself** has no binding to `SelectedFontSize` ✗
   - So Slider changes don't update `SelectedFontSize` property
   - `SelectedFontSize` changes don't update the Slider position

2. **In TextFormatDialog.xaml.cs**:
   - Code tries to bind `FontSizeSlider.Value` directly to manager properties ✓
   - But this works **only if** `FontManagerControl.SelectedFontSize` is properly managed
   - Since the Slider isn't bound in FontManager.xaml, this external binding creates a loop without proper two-way sync

### The Missing Link
**FontManager.xaml needs to bind the Slider to its SelectedFontSize property:**

```xaml
<!-- MISSING BINDING -->
<Slider x:Name="FontSizeSlider" 
	Value="{Binding SelectedFontSize, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=TwoWay}"
	... />
```

---

## Data Flow When Working Correctly

### Scenario: User moves slider in main window

```
User drags Slider
  ↓
Slider.Value changes (0-1)
  ↓ (via binding to SelectedFontSize property)
FontManager.SelectedFontSize property updates
  ↓ (via binding from LabelWiz.xaml to Diameter)
LabelManager.Diameter property updates
  ↓ (property setter calls ApplyScale())
ApplyScale() → MarkerLabel.Style.FontSize & Diameter updated
  ↓
TextBlock/Ellipse in Marker template listen to style changes
  ↓
Visual labels render with new size
```

### Scenario: User opens TextFormatDialog for Names

```
TextFormatDialog opens with NameManager as context
  ↓
TextFormatDialog.BindFontManager() called:
  - Binds FontManagerControl.FontColorProperty to NameManager.FontColor ✓
  - Binds FontManagerControl.BackgroundColorProperty to NameManager.BackgroundColor ✓
  - Binds FontManagerControl.FontSizeSlider.Value to NameManager.FontScale ✓ (programmatically)
  ↓
BUT: FontSizeSlider has no internal binding to SelectedFontSize
  ↓
Programmatic binding works, but:
  - Slider position updates FontScale property ✓
  - FontScale property setter calls ApplyScale() ✓
  - TextLabel.Style.FontSize updated ✓
  - Names render correctly ✓
```

**BUT wait—the programmatic binding actually bypasses the property, so it might work accidentally.**

---

## Architecture Layers Summary

| Layer | Component | Range | Purpose |
|-------|-----------|-------|---------|
| View | Slider UI | 0-1 | User interaction |
| Converter | SliderToScaleConverter | bidirectional | Map ranges |
| Model | Manager.FontScale/.Diameter | 0.25-4.0 | Control logic |
| Style | LabelStyle.FontSize, TextStyle.FontSize | pixels | Visual state |
| Render | XAML bindings | varies | Display |

---

## The Recent Simplification Issue

The architecture was recently simplified (as mentioned in the issue), which likely:

1. **Removed indirect bindings** that used to work through the property chain
2. **Left the Slider in FontManager.xaml unbound** to its SelectedFontSize property
3. Created a situation where:
   - LabelWiz.xaml binds to `SelectedFontSize` (property exists but unused)
   - TextFormatDialog tries to bind directly to Slider (bypass the property)
   - The Slider itself never updates the property or responds to property changes

---

## Verification: Base Value × Scale Factor Implementation

### ✓ CORRECT: Base Value Setup

The code correctly implements the base value × scale factor approach:

1. **Base Value Calculation** (in `LabelManager.SetLabels()`)
   ```csharp
   public void SetLabels(List<Rectangle> faces)
   {
       BaseLabelDiameter = SizingModel.ComputeBaseLabelDiameter(faces, _imageVM.Bitmap?.Width ?? 0);
       // BaseLabelFontSize is calculated from the baseline diameter
       RecalculateBaseLabelFontSize();  // Sets BaseLabelFontSize
   }
   ```

2. **Initialization Flow** (when fresh image loaded):
   - `FileManager.ExecuteOpenImage()` → loads bitmap
   - `WeakReferenceMessenger.Default.Send(new NewImageOpenedMessage(faces))` → triggered
   - `LabelManager` receives message → `SetLabels()` called
   - **Base values set**: `BaseLabelDiameter`, `BaseLabelFontSize`
   - `SettingsManager.ApplyFreshImageDefaults()` → sets scale factors to defaults

3. **For All Text Elements** (NameManager, TitleManager, ImageInfoManager, ImageIdManager):
   ```csharp
   private void ApplyScale()
   {
       var baseLabelFontSize = _labelManager.BaseLabelFontSize;  // ← Base value
       if (baseLabelFontSize <= 0) return;

       // Actual size = base × scale
       FontSize = SizingModel.ResolveSize(baseLabelFontSize, FontScale);  // ← Multiplication
   }
   ```

4. **The ResolveSize() Implementation** (in `SizingModel.cs`):
   ```csharp
   public static double ResolveSize(double baseSize, double scale)
   {
       if (!double.IsFinite(baseSize) || baseSize <= 0)
           return 0;

       // ← THIS IS THE KEY CALCULATION
       return baseSize * (double.IsFinite(scale) && scale > 0 ? scale : DefaultScale);
   }
   ```

### ✓ CORRECT: Base Values Properly Stored and Used

- **LabelManager**: 
  - `BaseLabelDiameter` (read-only, set in `SetLabels()`)
  - `BaseLabelFontSize` (read-only, calculated from diameter)
  - Updates `MarkerLabel.Style.Diameter` and `MarkerLabel.Style.FontSize` via `ApplyScale()`

- **NameManager, TitleManager, ImageInfoManager, ImageIdManager**:
  - All fetch `_labelManager.BaseLabelFontSize` in their `ApplyScale()` methods
  - All store their scale factor (0.25-4.0) in `FontScale` property
  - All calculate: `ActualFontSize = BaseLabelFontSize × FontScale`
  - All update shared style objects or their own `FontSize` property

### ✓ CORRECT: Scale Factor Management

When scale property changes:
```csharp
public double FontScale
{
    get => _fontScale;
    set
    {
        var clamped = Math.Clamp(value, 0.25, 4.0);
        if (_fontScale != clamped)
        {
            _fontScale = clamped;
            ApplyScale();  // ← Triggers recalculation
            OnPropertyChanged();
        }
    }
}
```

---

## Summary: What Needs to Be Fixed

**The BASE × SCALE implementation is CORRECT. The problem is only at the View-ViewModel boundary:**

1. **FontManager.xaml Slider** is not bound to its `SelectedFontSize` property
2. This breaks the binding chain between Slider and Manager properties
3. Base values work fine internally, but slider changes never reach the properties
4. **Fix**: Add missing binding in FontManager.xaml

### The Missing Binding

In `Views/FontManager.xaml`, the Slider needs to bind to the `SelectedFontSize` property:

```xaml
<!-- CURRENT (BROKEN): -->
<Slider x:Name="FontSizeSlider" Grid.Column="1"
    Minimum="0" Maximum="1"
    IsSnapToTickEnabled="False" />

<!-- NEEDED: -->
<Slider x:Name="FontSizeSlider" Grid.Column="1"
    Minimum="0" Maximum="1"
    IsSnapToTickEnabled="False"
    Value="{Binding SelectedFontSize, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=TwoWay}" />
```

This single binding will:
1. Propagate Slider changes to `SelectedFontSize` property
2. Propagate property changes back to Slider position
3. Enable the binding chain through `SliderToScaleConverter` to Manager properties
4. Trigger `ApplyScale()` which multiplies base × scale
5. Update style objects which trigger visual refresh

**The entire system is architecturally sound. It just needs this one missing data binding connection.**
