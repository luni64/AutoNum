# Fresh File Initialization Fix

## Problem
When opening a fresh image file, labels were not displayed unscaled. The user expected:
- Label scale factor = 1.0 (unscaled/neutral)
- Slider position = 0.5 (midpoint)

## Root Cause
The `ApplyFreshImageDefaults()` method in `SettingsManager.cs` was applying the saved default scale from previous sessions, rather than always applying the unscaled baseline for fresh files.

## Solution
Changed `SettingsManager.ApplyFreshImageDefaults()` to explicitly set the label scale to 1.0 for fresh images, instead of using the saved `DefaultLabelDiameterScale`:

```csharp
public void ApplyFreshImageDefaults(LabelManager labelManager, NameManager nameManager, TitleManager titleManager)
{
	// Fresh images should start unscaled (scale = 1.0, which is slider position 0.5)
	labelManager.LabelScale = 1.0;
	nameManager.FontScale = DefaultNamesFontScale;
	nameManager.IsEnabled = DefaultNamesEnabled;
	titleManager.FontScale = DefaultTitleFontScale;
	titleManager.IsEnabled = DefaultTitleEnabled;
}
```

## Math Verification
With the exponential slider mapping (`scale = 0.25 * 16^x`):
- When `scale = 1.0`: `x = log₁₆(1.0 / 0.25) = log₁₆(4) = 0.5`
- When slider = 0.5: `scale = 0.25 * 16^0.5 = 0.25 * 4 = 1.0` ✓

## Files Modified
- `AutoNum/ViewModels/SettingsManager.cs` - Updated `ApplyFreshImageDefaults()` to use explicit 1.0 scale

## Behavior
- **Before**: Opening a fresh file would apply the previously-saved scale, not an unscaled baseline
- **After**: Opening a fresh file always shows unscaled labels (scale=1.0) with slider at position 0.5
- **Metadata files**: When loading a file with saved metadata, the restored scale is applied (not affected by this change)

## Build Status
✓ Build successful
