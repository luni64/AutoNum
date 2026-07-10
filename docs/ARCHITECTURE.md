# AutoNum — Architecture Overview

## Purpose
AutoNum is a WPF desktop application (.NET 8, C# 12) that opens photos, detects faces via OpenCV (Emgu.CV), places numbered labels, and exports a numbered image with optional stacked text blocks (title, image information, image ID) and name list. It uses MahApps.Metro for the shell and dialogs.

## Project Structure

```
AutoNum/
├── Classifiers/                 # face_detection_yunet_2023mar.onnx (copied to output, PreserveNewest)
├── Infrastructure/             # Cross-cutting services/converters/messages
│   ├── Messages.cs             # WeakReferenceMessenger message types
│   ├── DialogService.cs        # Open/Save/error dialogs
│   ├── IDialogService.cs
│   └── Converters.cs           # WPF  converters (bitmap/color/visibility/etc.)
├── Model/                      # Persistence and image-processing helpers
│   ├── Analyzer.cs             # Text layout and measurement logic
│   ├── AutoNumMetaData.cs      # Metadata schema + JSON (V1-V5 router)
│   ├── AutoNumMetaData_V2.cs   # V2 metadata additions
│   ├── AutoNumMetaData_V3.cs   # V3 sizing anchors + relative scales
│   ├── AutoNumMetaData_V4.cs   # V4 persisted row-definition boundaries
│   ├── AutoNumMetaData_V5.cs   # V5 marker: label CenterX/CenterY are the true center (not top-left)
│   ├── FaceLabelAnchor.cs      # 3x3 anchor enum + fractional mapping for face-relative label placement
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
│   ├── SettingsWindow.xaml/.cs # Modal tabbed settings dialog (Datei → Einstellungen... in the main menu)
│   └── WizardViews/
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
- V5 metadata is a pure version marker: label `CenterX`/`CenterY` are the circle's true center (pre-V5 files stored the top-left corner instead). `ImageVM.InitFromMetadata` migrates pre-V5 positions by offsetting by half the saved label diameter so previously-saved images/PDFs render unchanged after reopening.

### Face-Relative Label Anchor
- `FaceLabelAnchor` (`Model/FaceLabelAnchor.cs`) is a 3x3-grid enum (Top/Middle/Bottom × Left/Center/Right) controlling where a freshly detected face's label is centered within the detected face rectangle.
- Applies only to newly created labels (open, redetect, rotate); existing labels are never moved retroactively when the setting changes.
- Persisted app-wide as `AppSettings.DefaultFaceLabelAnchor` and exposed as `SettingsManager.FaceLabelAnchor`; configured via a 3x3 radio-button grid in the Settings window's **Erkennung** tab, with a **Neu Erkennen** button beside it to re-run detection immediately with the new anchor.
- `LabelManager.SetLabels` resolves the anchor to a fractional position (`FaceLabelAnchor.ToFraction()`); `BottomCenter` keeps the historical slight overshoot below the chin, all other anchors sit exactly on the rectangle's fraction point.

### File Menu Commands
- `MainWindow.xaml` hosts a top `Menu` (Datei: Öffnen/Speichern/Speichern unter/Metadaten exportieren/Einstellungen; Hilfe: Handbuch) instead of the old left-panel buttons — there is no left column in the main grid anymore, and no title-bar gear button (Einstellungen now lives in the Datei menu, `MainWindow.xaml.cs`'s `OpenSettings_Click`).
- `FileManager.ResolveOutputFolder(sourcePath)` decides the Save-As/Export-metadata dialog's initial directory from `SettingsManager.UseCustomOutputFolder`/`OutputFolder`: an absolute (`Path.IsPathFullyQualified`) folder must already exist (set via the Browse button); a relative folder (e.g. `AutoNum`, or `/AutoNum` — leading separators are trimmed) is created on demand as a subfolder next to the source image. Everything is normalized through `Path.GetFullPath` before being handed to `SaveFileDialog.InitialDirectory`, since the underlying shell API throws on a mixed-separator path (e.g. `C:\Photos\AutoNum/test`) even though `Directory.CreateDirectory` tolerates it.
- `DialogService.ShowDialog` passes `SaveFileInfo`/`OpenFileInfo.FilterIndex` through to the native dialog's `FilterIndex`, used to preselect the JPG/PDF filter in Save As per `SettingsManager.DefaultSaveFormat`.

## Data Flow

### Open fresh image (no AutoNum metadata)
1. `FileManager` loads bitmap and applies EXIF orientation.
2. `ImageVM` is initialized.
3. If `SettingsManager.FaceDetectionEnabled` is true, fresh-image face detection runs via `FaceDetector` and `NewImageOpenedMessage` triggers `LabelManager.SetLabels(...)`.
4. `LabelManager.SetLabels(...)` initializes persons, computes baseline label diameter, and optionally assigns rows when `SettingsManager.RowDetectionEnabled` is true.
5. `SettingsManager.ApplyFreshImageDefaults(...)` ensures all managers start unscaled:
   - All managers (`LabelManager`, `NameManager`, `TitleManager`, `ImageInfoManager`, `ImageIdManager`) have `FontScale = 1.0`
   - Applies saved default toggles for names, title, and image-info visibility
   - Slider positions are all at 0.5 (unscaled baseline)
   - Fresh-image detection defaults are enabled unless the user changes them in the settings dialog

### Open saved AutoNum image
1. Metadata is loaded from either:
   - JPEG EXIF UserComment (`_num.jpg`), or
   - embedded PDF payload zip (`_num.pdf`) via `PdfPayloadStore`.
2. For JPEG V2/V3, the clean base image is reconstructed from embedded patches (`AppSegmentIO`/`RestoreFromPatches`). For editable PDF payloads, the clean base image is read directly from the embedded payload image.
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

### Save / Save As / Export Metadata (Datei menu)
All three are `FileManager` commands/methods bound directly from `MainWindow.xaml`'s `Menu` (no left-panel buttons; see Key Design Patterns → File Menu Commands below):
1. **Speichern** (`SaveCommand`) writes back to `ImageVM.CurrentImageFilename` in place, no dialog. `CanExecute` is `!IsProtectedOriginalPath(CurrentImageFilename, OriginalImageFilename)` — disabled until the current file is no longer the protected original (i.e. until the first Save As), re-evaluated via `CommandManager.InvalidateRequerySuggested()` after every open/save.
2. **Speichern unter...** (`SaveAsCommand`) shows a single Save dialog with a combined JPEG+PDF filter (`SettingsManager.DefaultSaveFormat` only controls which filter is preselected); the actual format written is decided from the extension of the path the user picks/types.
3. Both dispatch to `FileManager.WriteJpgOrPdf(filename)`, which routes to `WriteJpg`/`WritePdfWithSidecars` by extension, updates `CurrentImageFilename`, and writes CSV/JSON sidecars per `ExportCsvMetadata`/`ExportJsonMetadata` (auto-export-alongside-save toggles).
4. **Metadaten exportieren...** (`FileManager.ExportMetadataNow()`) is a separate on-demand action: its own Save dialog (CSV/JSON filter, format by extension), fully decoupled from the `ExportCsvMetadata`/`ExportJsonMetadata` toggles above — those only govern what rides along with Save/Save As, never whether this menu item works.
5. Rendering: `ToNumberedBitmap(...)` composites optional stacked title, image-information, image-ID, and names blocks (order: Title, Information, Image, ID, Names). JPEG path injects APP4 patches and embeds `Version = "V3"` metadata; PDF path embeds a versioned AutoNum payload zip (`metadata + base image`) as a standard PDF attachment for round-trip editing from `_num.pdf`.
6. Prevent overwrite only for the protected original file path (`IsProtectedOriginalPath`), checked after the Save As dialog returns.

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
4. `LabelManager` also acts as the row/label orchestration point for fresh-image detection and manual re-detection, while `RowDefinitionManager` owns row preview/edit mode and row-count transitions.

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
- `SettingsManager` exposes bindable settings in `SettingsWindow` (modal dialog opened via Datei → Einstellungen... in the main menu, `MainWindow.xaml.cs`'s `OpenSettings_Click`).
- Four tabs:
  - **Formatierung**: scale factor sliders for numbers, title, description (image info), image-ID, and names fonts (0.25–4.0 range via exponential mapping), plus colors; "Auf das aktuelle Bild anwenden" restores all saved defaults to the current image; per-element "Use as default" buttons in formatting dialogs save individual element scales.
  - **Sichtbarkeit**: default show/hide toggles for title, description, image-ID, and names list on newly opened images; "Anwenden" applies them to the current image.
  - **Erkennung** (Detection): Face detection enable/disable, row detection enable/disable, the face-relative label anchor (3x3 grid, see Face-Relative Label Anchor above), and a manual "Neu Erkennen" action that re-runs face detection. The detector (YuNet, see External Dependencies) has no user-configurable tuning parameters.
  - **Export**: save-file suffix (`SaveFileSuffix`), custom output folder (`UseCustomOutputFolder`/`OutputFolder`, absolute or relative — see File Menu Commands above), CSV/JSON auto-export-alongside-save toggles (`ExportCsvMetadata`/`ExportJsonMetadata`), and `DefaultSaveFormat` (JPG/PDF radio buttons controlling which filter is preselected in the Speichern-unter dialog).
- Scope:
  - affects **new fresh-image sessions** and detector/save defaults
  - detection defaults are enabled by default for new and migrated settings
  - does **not** override per-image values restored from metadata

## Rendering Notes
- **Live preview renderer (WPF/XAML):** marker templates in `Marker.xaml` render label circles and names-table rows. Holding **Ctrl** while dragging a label (`Marker.xaml.cs`) moves all other labels by the same delta, for quick bulk repositioning.
- **Name-list hover highlight:** `Person.IsSelected` drives a thick magenta stroke on the corresponding label (`Marker.xaml`'s `RowPreviewEdgeConverter`/`SelectedStrokeThicknessConverter`, sized as a fraction of `Diameter` so it scales with label size). Set by `Infrastructure/PersonHoverHighlight.cs`, an attached behavior (`is:PersonHoverHighlight.Enable`) on the Namensliste `DataGrid` (`NamesView.xaml`) that hit-tests under the cursor on every `PreviewMouseMove` and diffs against the previously-hovered `Person` — deliberately not per-row `MouseEnter`/`MouseLeave`, which can desync (leaving a stale highlight) when the mouse moves fast across recycled row containers.
- **JPG export renderer (GDI+):** `ExtensionMethods` draws final bitmap; label drawing uses supersampled anti-aliased overlay/downsampling for improved small-label quality.
- **PDF export renderer (QuestPDF):** `FileManager.WritePdf(...)` creates document output, sets standard PDF document metadata, and embeds editable payload as a non-visible PDF attachment. The numbered image is embedded as **PNG** (not JPEG): JPEG embedding causes QuestPDF to write `/ColorTransform 0` alongside an `ICCBased` colorspace, which confuses Acrobat DC's tile cache and makes the image vanish at 100% zoom on scroll.
- Save operations use retry/cancel prompting when a target file is locked by another application, allowing the user to close the conflicting program and try again.
- Names-table row geometry is computed once via `NameTableLayoutEngine` and projected to `TextLabel` row bounds (`X/Y/W/H`) so preview and JPG share the same wrap-aware layout foundation.
- To minimize drift between renderers, column-width and padding rules are centralized in `NamesTableLayout`; preview uses dedicated converters, JPG uses GDI drawing helpers, and PDF uses the same width resolver.
- Names-table measurement/rendering paths use pixel-based GDI font units to avoid WPF/GDI point-vs-pixel mismatch.

## External Dependencies
- **Emgu.CV** — face detection via `FaceDetectorYN` (YuNet ONNX model, `Classifiers/face_detection_yunet_2023mar.onnx`); fixed score/NMS thresholds, not user-configurable
- **MahApps.Metro** — WPF shell + dialogs
- **CommunityToolkit.Mvvm** — messenger only
- **QuestPDF** — PDF rendering and standards-compliant PDF attachment embedding
- **PdfPig** — extraction of embedded PDF payload attachments on import

## Conventions
- C# 12 / .NET 8
- MVVM, no business logic in code-behind
- `is null` / `is not null`, `nameof(...)`, PascalCase public API
- Ongoing release notes in `docs/release/NEXT_RELEASE.md`
