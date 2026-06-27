# Settings Dialog Scale Sliders Enhancement

## Overview
Updated the Settings dialog to allow users to configure default scale factors for all text elements (names, title, image-info, image-ID), and added a convenient button to read current values from the main screen.

## Changes Made

### 1. AppSettings Model (`AutoNum/Model/AppSettings.cs`)
- Added two new properties:
  - `DefaultImageInfoFontSlider` (defaults to 0.5)
  - `DefaultImageIdFontSlider` (defaults to 0.5)

### 2. SettingsManager (`AutoNum/ViewModels/SettingsManager.cs`)

#### New Properties
- `DefaultImageInfoFontScale` - Scale factor for image-info text (0.25–4.0)
- `DefaultImageIdFontScale` - Scale factor for image-ID text (0.25–4.0)

#### New Command & Method
- `ReadCurrentValuesCommand` - RelayCommand that reads current scale values from the main window
- `ExecuteReadCurrentValues()` - Captures current scales from:
  - `NameManager.FontScale`
  - `TitleManager.FontScale`
  - `ImageInfoManager.FontScale`
  - `ImageIdManager.FontScale`
  - Automatically saves the new defaults to settings

#### Updated Methods
- Constructor: Loads new slider values and converts them to scales
- `ApplyFreshImageDefaults()`: Now applies saved defaults instead of hardcoded 1.0:
  - Labels always start at 1.0 (unscaled)
  - Names, title, image-info, image-ID use their saved default scales
- `SaveSettings()`: Persists new scale values to settings file

### 3. General Settings View UI (`AutoNum/Views/WizardViews/GeneralSettingsView.xaml`)

#### Removed
- Visibility toggles for names and title (kept in the code but removed from UI)
- Label size slider (labels always start at 1.0, not user-configurable)

#### Added
- Four scale factor sliders (with labels):
  - "Namensliste-Schrift (Slider)" - Names font scale
  - "Titelschrift (Slider)" - Title font scale
  - "Bildinformations-Schrift (Slider)" - Image-info font scale
  - "Bild-ID-Schrift (Slider)" - Image-ID font scale
- "Aktuelle Werte vom Bildschirm übernehmen" (Read current values from screen) button
  - Reads current scales from the main window and saves as defaults
  - Uses `CommandParameter` to pass the main window's DataContext (MainVM)

#### Reorganized
- Moved "Erkennung und Speichern" section below the new sliders

## User Workflow

1. **Adjust scales on an example image**: User opens an image and adjusts the scale sliders for each text element to their preferred sizes
2. **Capture as defaults**: Click "Read current values from screen" button
3. **Save**: Settings are automatically saved to `%AppData%/AutoNum/settings.json`
4. **Fresh images**: All subsequent fresh images will use these saved default scales

## Technical Details

### Scale Conversion
- UI uses exponential slider mapping (0–1) ↔ scale (0.25–4.0)
- Defaults are stored as slider positions (0–1) in JSON
- Converted to scale values (0.25–4.0) on load
- Converted back to slider positions on save for persistence

### Label Scale Behavior
- **Labels always start at 1.0 (unscaled)** on fresh images
- This is intentional: labels are the base unit, other elements scale relative to them
- Not user-configurable through defaults

### Metadata Precedence
- Saved AutoNum images restore their per-image scales from metadata
- Settings defaults only apply to fresh images
- No override of metadata-loaded values

## Files Modified
- `AutoNum/Model/AppSettings.cs` - Added new settings properties
- `AutoNum/ViewModels/SettingsManager.cs` - Added scale properties and read-current-values command
- `AutoNum/Views/WizardViews/GeneralSettingsView.xaml` - Updated UI layout and bindings
- `docs/ARCHITECTURE.md` - Updated documentation
- `docs/release/NEXT_RELEASE.md` - Updated release notes

## Build Status
✓ Successful
