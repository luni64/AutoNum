# AutoNum — Architecture Overview

## Purpose
AutoNum is a WPF desktop application (.NET 8, C# 12) that opens photos, detects faces via OpenCV (Emgu.CV), places numbered labels, and exports a numbered image with optional stacked text blocks (title, image information, image ID) and name list. It uses MahApps.Metro for the shell and dialogs.

## Project Structure

```
AutoNum/
├── Infrastructure/             # Cross-cutting services/converters/messages
│   ├── Messages.cs             # WeakReferenceMessenger message types
│   ├── DialogService.cs        # Open/Save/error dialogs
│   ├── IDialogService.cs
│   └── Converters.cs           # WPF  converters (bitmap/color/visibility/etc.)
├── Model/                      # Persistence and image-processing helpers
│   ├── Analyzer.cs             # Text layout and measurement logic
│   ├── AutoNumMetaData.cs      # Metadata schema + JSON (V1/V2/V3 router)
│   ├── AutoNumMetaData_V2.cs   # V2 metadata additions
│   ├── AutoNumMetaData_V3.cs   # V3 sizing anchors + relative scales
│   ├── AppSettings.cs          # App-wide settings model + %AppData% JSON store
│   ├── BitmapExtensions.cs     # EXIF read/write/orientation + patch restore
│   ├── ExtensionMethods.cs     # Render/export pipeline
│   ├── AppSegmentIO.cs         # JPEG APP4 segment read/write for patches
│   ├── SizingModel.cs          # Shared label/name/title baseline + scale math
│   ├── PatchData.cs            # Patch payload model
│   ├── PdfPayloadContract.cs   # PDF-embedded payload schema (manifest + entries)
│   ├── PdfPayloadStore.cs      # PDF payload zip create/read + embed/extract
│   ├── NamesTableLayout.cs     # Shared names-table contracts/options and sizing constants
│   ├── NameTableLayoutEngine.cs# Shared names-table layout computation (wrap-aware row geometry)
│   └── FontFamilyResolver.cs   # Safe metadata font-family restore with fallback logging
├── ViewModels/                 # MVVM view models (INotifyPropertyChanged)
│   ├── MainVM.cs               # Composition root for managers/view models
│   ├── ImageVM.cs              # Loaded bitmap, persons, zoom/pan, file paths
│   ├── FileManager.cs          # Open/save orchestration + overwrite protection
│   ├── LabelManager.cs         # Number labels styling and numbering
│   ├── NameManager.cs          # Name list behavior and layout
│   ├── TitleManager.cs         # Title behavior and styling
│   ├── ImageInfoManager.cs     # Secondary image-information banner behavior and styling
│   ├── ImageIdManager.cs       # Image ID behavior and styling
│   ├── SettingsManager.cs      # App-wide defaults (persisted settings)
│   ├── FaceDetector.cs         # Static OpenCV detector configuration/execution
│   ├── Person.cs, MarkerVM.cs, MarkerRect.cs, TextLabel.cs
│   └── LabelStyle.cs, TextStyle.cs
├── Views/
│   ├── MainWindow.xaml/.cs
│   ├── PictureDisplay.xaml/.cs
│   ├── FontManager.xaml/.cs
│   ├── Marker.xaml/.cs
│   ├── ZoomBorder.cs
│   ├── SettingsWindow.xaml/.cs # Modal tabbed settings dialog (gear in title bar)
│   └── WizardViews/
│       ├── FilesView.xaml
│       ├── LabelWiz.xaml
│       ├── NamesView.xaml
│       ├── ImageInfoView.xaml
│       ├── ImageIdView.xaml
│       └── TiltleView.xaml
└── docs/release/
    ├── NEXT_RELEASE.md
    ├── CHANGELOG.md
    └── RELEASE_PROCESS.md
```

## Key Design Patterns

### MVVM + Messenger
- Views bind to ViewModels only; no View references inside ViewModels.
- Cross-VM events use `CommunityToolkit.Mvvm.WeakReferenceMessenger`:
  - `NewImageOpenedMessage` (fresh image loaded)
  - `MetadataLoadedMessage` (saved AutoNum image restored)
  - `LabelsChangedMessage` (renumber/layout refresh)

### Composition Root in `MainVM`
`MainVM` constructs and exposes `ImageVM`, `FileManager`, `LabelManager`, `NameManager`, `TitleManager`, `ImageInfoManager`, `ImageIdManager`, and `SettingsManager`.

### Shared Style Objects
`MarkerLabel.Style` (`LabelStyle`) and `TextLabel.Style` (`TextStyle`) hold shared visual settings and notify through weak subscriptions.

### Scale-Factor Sizing Model
- Label diameter baseline is computed once from detected face size (or an image-width fallback).
- Label font 100% baseline is fitted to that baseline diameter.
- Name and title font 100% baselines reuse the fitted label font baseline.
- **Scale factors** (0.25–4.0) are applied to these baselines to derive displayed sizes:
  - `ResolveSize(baseSize, scale) = baseSize * scale`
  - Unscaled (neutral) state is always `scale = 1.0`
- UI sliders represent scale values via exponential mapping:
  - `scale = 0.25 * 16^(slider_position)` (slider position is 0–1)
  - Slider position 0.5 corresponds to `scale = 1.0` (unscaled)
  - Slider position 0.0 corresponds to `scale = 0.25` (25% of base)
  - Slider position 1.0 corresponds to `scale = 4.0` (400% of base)
- V3 metadata stores both exact anchors and relative scales so reopen is deterministic while V1/V2 files migrate through legacy size ratios.

## Data Flow

### Open fresh image (no AutoNum metadata)
1. `FileManager` loads bitmap and applies EXIF orientation.
2. Faces are detected via `FaceDetector`.
3. `ImageVM` is initialized; `NewImageOpenedMessage` triggers `LabelManager.SetLabels(...)`.
4. `LabelManager.SetLabels(...)` initializes persons, computes baseline label diameter, and sets `LabelScale = 1.0` (unscaled).
5. `SettingsManager.ApplyFreshImageDefaults(...)` ensures all managers start unscaled:
   - All managers (`LabelManager`, `NameManager`, `TitleManager`, `ImageInfoManager`, `ImageIdManager`) have `FontScale = 1.0`
   - Applies saved default toggles for names, title, and image-info visibility
   - Slider positions are all at 0.5 (unscaled baseline)

### Open saved AutoNum image
1. Metadata is loaded from either:
   - JPEG EXIF UserComment (`_num.jpg`), or
   - embedded PDF payload zip (`_num.pdf`) via `PdfPayloadStore`.
2. For JPEG V2/V3 and editable PDF payloads, the clean base image is reconstructed from embedded patches (`AppSegmentIO`/`RestoreFromPatches` for JPEG, payload patches for PDF).
3. `ImageVM.InitFromMetadata(...)` rebuilds persons and publishes `MetadataLoadedMessage`.
4. Managers restore styling/toggles/scales/font families from metadata:
   - V3 restores exact sizing anchors/scales
   - V1/V2 migrate legacy absolute sizes via stored ratios for visually equivalent results
   - Label, names, title, image-info, and image-ID font families are restored with safe fallback (`FontFamilyResolver`) when unavailable on the current system.
   - Names-table column count is restored per image (`NamesColumnCount`, clamped 1..4; missing legacy value falls back to 1).
5. After label baseline restore, `LabelManager` emits `LabelsChangedMessage` so dependent managers (`NameManager`, `ImageIdManager`) reapply scale against the restored base and avoid transient under-scaled preview.

### Rotate image (90° clockwise)
1. User triggers rotate from the left **Bild** action group in `MainWindow`.
2. `LabelManager.RotateImageCommand` checks whether names are present and, if needed, shows the same delete-names warning used by the delete-label flow.
3. On confirmation, existing persons (labels/names) are cleared, bitmap pixels are rotated (`RotateFlipType.Rotate90FlipNone`), and `ImageVM.Init()` refreshes image dimensions/fit state.
4. Face detection is re-run on the rotated bitmap, then `SetLabels(...)` recreates labels in the rotated coordinate space.
5. `LabelsChangedMessage` refreshes dependent layout/scale consumers so preview and export stay consistent.

### Save image
1. `FileManager` proposes output name:
   - protected original + setting enabled => suggest `_num`
   - otherwise suggest current filename
2. Prevent overwrite only for the protected original file path.
3. Render with `ToNumberedBitmap(...)`, including optional stacked title, image-information, image-ID, and names blocks in order: Title, Information, Image, ID, Names.
4. Save JPEG bytes, inject APP4 patches, embed metadata as `Version = "V3"` with exact sizing anchors plus relative scales.
5. Optional PDF export path renders the PDF document and appends a versioned embedded AutoNum payload zip (metadata + composite + patches) for round-trip editing from `_num.pdf`.

## Slider & Scale Control Architecture

### Reusable FontManager Control
- `FontManager.xaml/.cs` is a reusable UI control containing a slider and color pickers.
- Exposes `SelectedScale` (double) as a dependency property (range 0.25–4.0, default 1.0).
- Slider in XAML is bound two-way to `SelectedScale` through `SliderToScaleConverter`:
  - Forward (VIEW → MODEL): slider position (0–1) → scale (0.25–4.0) via UI-layer slider mapping (`SliderScaleMapping`)
  - Reverse (MODEL → VIEW): scale (0.25–4.0) → slider position (0–1) via UI-layer slider mapping (`SliderScaleMapping`)
- Used in three contexts:
  1. **Main window label wizard** (`LabelWiz.xaml`): binds `SelectedScale` to `LabelManager.LabelScale`
  2. **Text-format dialogs** (`TextFormatDialog.xaml.cs`): dynamically binds `SelectedScale` to whichever manager is open (TitleManager, ImageInfoManager, ImageIdManager, NameManager)
  3. **Settings window** (`SettingsWindow.xaml.cs`): binds to app-wide default scales

### Scale Propagation
Each manager that uses scale (LabelManager, NameManager, TitleManager, ImageInfoManager, ImageIdManager) follows the same pattern:
1. Holds a `FontScale` property (0.25–4.0).
2. Stores a `BaseFontSize` or `BaseLabelDiameter` computed from the image or fitted text.
3. Calls `ApplyScale()` when scale changes, which recomputes visible sizes:
   - `visibleFontSize = ResolveSize(baseFontSize, scale)` = `baseFontSize * scale`
   - Updates the corresponding UI style (e.g., `MarkerLabel.Style.FontSize`)

### Fresh-Image & Settings Initialization
- When a fresh image opens, `LabelManager.SetLabels()` sets `LabelScale = 1.0` (always unscaled).
- `SettingsManager.ApplyFreshImageDefaults()` applies saved default scale factors to other managers:
  - `LabelManager.LabelScale = 1.0` (labels always unscaled)
  - `NameManager.FontScale = DefaultNamesFontScale`
  - `TitleManager.FontScale = DefaultTitleFontScale`
  - `ImageInfoManager.FontScale = DefaultImageInfoFontScale`
  - `ImageIdManager.FontScale = DefaultImageIdFontScale`
- Users can adjust these defaults in two ways:
  - **Per-element capture**: Open a formatting dialog (from the right-column UI), adjust the scale with the slider, click "Als Standard übernehmen" (Use as default) to save that element's current scale
  - **Batch apply**: Open Settings → Schriften tab, adjust default sliders, click "Anwenden" (Apply) to restore all saved defaults to the current image
- Scale values are displayed as percentages (e.g., "100%", "150%", "75%") next to sliders in both the Settings dialog and formatting dialogs
- Defaults are persisted in `%AppData%/AutoNum/settings.json`
- Visibility toggles for names, title, and image-info are set from saved defaults.
- When a saved AutoNum image is loaded, metadata restores per-image scale overrides and visibility settings instead.

## Settings Architecture
- App-wide defaults are persisted in `%AppData%/AutoNum/settings.json`.
- `SettingsManager` exposes bindable settings in `SettingsWindow` (modal dialog opened from a title-bar gear command).
- **Schriften (Fonts) tab** allows users to configure:
  - Explanatory text describing how the app determines base font size from image resolution, and that slider values are factors relative to this base
  - Scale factor sliders for title, description (image info), image-ID, and names fonts (0.25–4.0 range via exponential mapping)
  - Percentage display next to each slider showing the current scale factor (100% = base size)
  - "Anwenden" (Apply) button to restore all saved defaults to the current image
  - Per-element "Use as default" buttons in formatting dialogs to save individual element scales
- Other tabs:
  - **Erkennung** (Detection): Face detection sensitivity and neighborhood settings
  - **Speichern** (Save): Save-file naming convention toggle
- Scope:
  - affects **new fresh-image sessions** and detector/save defaults
  - does **not** override per-image values restored from metadata

## Rendering Notes
- **Live preview renderer (WPF/XAML):** marker templates in `Marker.xaml` render label circles and names-table rows.
- **JPG export renderer (GDI+):** `ExtensionMethods` draws final bitmap; label drawing uses supersampled anti-aliased overlay/downsampling for improved small-label quality.
- **PDF export renderer (QuestPDF):** `FileManager.WritePdf(...)` creates document output; editable payload is embedded separately and is not page-visible.
- Names-table row geometry is computed once via `NameTableLayoutEngine` and projected to `TextLabel` row bounds (`X/Y/W/H`) so preview and JPG share the same wrap-aware layout foundation.
- To minimize drift between renderers, column-width and padding rules are centralized in `NamesTableLayout`; preview uses dedicated converters, JPG uses GDI drawing helpers, and PDF uses the same width resolver.
- Names-table measurement/rendering paths use pixel-based GDI font units to avoid WPF/GDI point-vs-pixel mismatch.

## External Dependencies
- **Emgu.CV** — face detection
- **MahApps.Metro** — WPF shell + dialogs
- **CommunityToolkit.Mvvm** — messenger only
- **QuestPDF** — PDF rendering

## Conventions
- C# 12 / .NET 8
- MVVM, no business logic in code-behind
- `is null` / `is not null`, `nameof(...)`, PascalCase public API
- Ongoing release notes in `docs/release/NEXT_RELEASE.md`
