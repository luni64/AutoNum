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
│   └── Converters.cs           # WPF converters (bitmap/color/visibility/etc.)
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
│   └── PatchData.cs            # Patch payload model
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

### Relative Sizing Model
- Label diameter baseline is computed once from detected face size (or an image-width fallback).
- Label font 100% baseline is fitted to that baseline diameter.
- Name and title font 100% baselines reuse the fitted label font baseline.
- UI sliders now represent scale percentages (25%–200%) around those baselines.
- V3 metadata stores both exact anchors and relative scales so reopen is deterministic while V1/V2 files migrate through legacy size ratios.

## Data Flow

### Open fresh image (no AutoNum metadata)
1. `FileManager` loads bitmap and applies EXIF orientation.
2. Faces are detected via `FaceDetector`.
3. `ImageVM` is initialized; `NewImageOpenedMessage` triggers label/name setup.
4. `SettingsManager.ApplyFreshImageDefaults(...)` applies app default toggles/sliders.

### Open saved AutoNum image
1. Metadata is read from EXIF UserComment.
2. V2/V3 path restores a clean base image from embedded APP4 patches (`AppSegmentIO` + `RestoreFromPatches`).
3. `ImageVM.InitFromMetadata(...)` rebuilds persons and publishes `MetadataLoadedMessage`.
4. Managers restore styling/toggles/font sizes from metadata:
   - V3 restores exact sizing anchors/scales
   - V1/V2 migrate legacy absolute sizes via stored ratios for visually equivalent results
   - `TitleManager`, `ImageInfoManager`, and `ImageIdManager` independently restore text, visibility, and style settings for their stacked blocks

### Save image
1. `FileManager` proposes output name:
   - protected original + setting enabled => suggest `_num`
   - otherwise suggest current filename
2. Prevent overwrite only for the protected original file path.
3. Render with `ToNumberedBitmap(...)`, including optional stacked title, image-information, image-ID, and names blocks in order: Title, Information, Image, ID, Names.
4. Save JPEG bytes, inject APP4 patches, embed metadata as `Version = "V3"` with exact sizing anchors plus relative scales.

## Settings Architecture
- App-wide defaults are persisted in `%AppData%/AutoNum/settings.json`.
- `SettingsManager` exposes bindable settings in `SettingsWindow` (modal dialog opened from a title-bar gear command).
- Scope:
  - affects **new fresh-image sessions** and detector/save defaults
  - does **not** override per-image values restored from metadata

## External Dependencies
- **Emgu.CV** — face detection
- **MahApps.Metro** — WPF shell + dialogs
- **CommunityToolkit.Mvvm** — messenger only

## Conventions
- C# 12 / .NET 8
- MVVM, no business logic in code-behind
- `is null` / `is not null`, `nameof(...)`, PascalCase public API
- Ongoing release notes in `docs/release/NEXT_RELEASE.md`
