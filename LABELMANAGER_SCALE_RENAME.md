# Consistency Fix: LabelManager.Diameter → LabelManager.LabelScale

## Summary
Successfully renamed `LabelManager.Diameter` to `LabelManager.LabelScale` for consistency with all other managers (NameManager, TitleManager, ImageInfoManager, ImageIdManager).

## Why This Was Needed
The property `Diameter` was **misleading** because:
- It's a **scale factor** (0.25-4.0), not an actual diameter value
- Other managers use `FontScale` for the same concept
- This caused confusion in the binding: `SelectedScale="{Binding Diameter}"`

Now it's clear: `SelectedScale="{Binding LabelScale}"`

## Changes Made

### 1. LabelManager.cs
**Property renamed:**
```csharp
// Before:
public double Diameter { get; set; }
private double _diameter = 1.0;

// After:
public double LabelScale { get; set; }
private double _labelScale = 1.0;
```

**All internal usages updated:**
- `SetLabels()` method: `LabelScale = 1.0`
- `MetadataLoadedMessage` handler: `LabelScale = v3.LabelScale` and `LabelScale = 1.0`
- `ApplyScale()` method: Uses `LabelScale` in calculations
- `ApplyScale()` debug statement: Updated field reference

### 2. LabelWiz.xaml
**Binding updated:**
```xaml
<!-- Before: -->
SelectedScale="{Binding Diameter}"

<!-- After: -->
SelectedScale="{Binding LabelScale}"
```

### 3. MainWindow.xaml.cs
**TextFormatDialog parameter updated:**
```csharp
// Before:
LabelManager manager => new TextFormatDialog(..., nameof(LabelManager.Diameter))

// After:
LabelManager manager => new TextFormatDialog(..., nameof(LabelManager.LabelScale))
```

### 4. SettingsManager.cs
**Property assignment updated:**
```csharp
// Before:
labelManager.Diameter = DefaultLabelDiameterScale;

// After:
labelManager.LabelScale = DefaultLabelDiameterScale;
```

## Files Changed
1. ✅ `AutoNum/ViewModels/LabelManager.cs` - Property renamed, all usages updated
2. ✅ `AutoNum/Views/WizardViews/LabelWiz.xaml` - Binding updated
3. ✅ `AutoNum/Views/MainWindow.xaml.cs` - Format dialog parameter updated
4. ✅ `AutoNum/ViewModels/SettingsManager.cs` - Property assignment updated

## Build Status
✅ **Build successful** - No compilation errors

## Benefits
- **Consistency**: All scale properties now follow naming convention (LabelScale, FontScale, etc.)
- **Clarity**: Property name correctly indicates it's a scale factor, not a diameter
- **Maintainability**: Code is now self-documenting
- **No functional changes**: The behavior is identical, only the naming is improved

## Related Properties (NOT Changed)
- `MarkerLabel.Style.Diameter` - This is the actual visual diameter (in pixels), correctly named
- `BaseLabelDiameter` - This is the base diameter before scaling (in pixels), correctly named
- `LabelManager.BaseLabelDiameter` - Read-only, correctly named

---

## Architecture Consistency Check

| Manager | Scale Property | Type | Range | Comment |
|---------|---|---|---|---|
| LabelManager | `LabelScale` | Scale factor | 0.25-4.0 | ✅ Now consistent |
| NameManager | `FontScale` | Scale factor | 0.25-4.0 | ✅ Consistent |
| TitleManager | `FontScale` | Scale factor | 0.25-4.0 | ✅ Consistent |
| ImageInfoManager | `FontScale` | Scale factor | 0.25-4.0 | ✅ Consistent |
| ImageIdManager | `FontScale` | Scale factor | 0.25-4.0 | ✅ Consistent |

✅ **All managers now follow consistent naming convention**
