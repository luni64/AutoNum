# AutoNum — Architecture Overview

## Purpose
AutoNum is a WPF desktop application (.NET 8, C# 12) that opens a photo, automatically detects faces using OpenCV (Emgu.CV), places numbered labels on each person, and exports a numbered copy with optional name list and title. It uses MahApps.Metro for the UI shell.

## Project Structure

```
AutoNum/
├── Infrastructure/         # Cross-cutting: dialogs, converters, messages
│   ├── Messages.cs         # WeakReferenceMessenger message types
│   ├── DialogService.cs    # IDialogService implementation (Open/Save dialogs)
│   ├── IDialogService.cs
│   └── Converters.cs       # WPF value converters (Bitmap↔BitmapSource, Color, Font, etc.)
├── Model/                  # Domain/persistence logic (no UI concerns)
│   ├── Analyzer.cs         # Text measurement, name/title layout placement
│   ├── AutoNumMetaData.cs  # JSON metadata (V1) serialized into EXIF UserComment tag
│   ├── BitmapExtensions.cs # Read/write EXIF metadata, apply EXIF orientation
│   └── ExtensionMethods.cs # Bitmap rendering: draw labels, names, title → export image
├── ViewModels/             # MVVM ViewModels — all implement INotifyPropertyChanged
│   ├── BaseViewmodel.cs    # Hand-rolled INPC base, RelayCommand, AsyncCommand
│   ├── MainVM.cs           # Root ViewModel / DataContext for MainWindow
│   ├── ImageVM.cs          # Holds the loaded Bitmap, Persons collection, zoom/pan state
│   ├── FileManager.cs      # Open/Save commands, image loading, face detection orchestration
│   ├── LabelManager.cs     # Label colors, diameter slider, numbering algorithm
│   ├── NameManager.cs      # Name list visibility, font size slider, layout
│   ├── TitleManager.cs     # Title text, font size, colors
│   ├── FaceDetector.cs     # Static OpenCV face detection (Haar cascade, lazy-cached)
│   ├── Person.cs           # Pairs a MarkerLabel + TextLabel per detected person
│   ├── MarkerVM.cs         # Base for draggable canvas markers (X, Y, W, H)
│   ├── MarkerRect.cs       # MarkerLabel: numbered circle label (extends MarkerVM)
│   ├── TextLabel.cs        # Name text label (extends MarkerVM)
│   ├── LabelStyle.cs       # Shared label styling (Diameter, FontSize, colors) — INPC
│   └── TextStyle.cs        # Shared name styling (FontSize, FontColor) — INPC
├── Views/                  # WPF Views (XAML + minimal code-behind)
│   ├── MainWindow.xaml/.cs # MahApps MetroWindow, sets DataContext = MainVM
│   ├── PictureDisplay.xaml/.cs  # Canvas with image + draggable markers
│   ├── Marker.xaml/.cs     # DataTemplate-driven marker (Label ellipse or Name text)
│   ├── FontManager.xaml/.cs # Reusable color/font-size control
│   ├── ZoomBorder.cs       # Mouse zoom/pan + right-click to add person
│   └── S1_SelectFile.xaml  # (Unused placeholder)
└── docs/release/
    ├── NEXT_RELEASE.md     # Scratchpad for upcoming release notes
    ├── CHANGELOG.md        # Released version history
    └── RELEASE_PROCESS.md  # Tagging/publishing steps
```

## Key Design Patterns

### MVVM with Message Bus
- ViewModels never reference Views. Views bind via `{Binding}` in XAML.
- **Cross-VM communication** uses `CommunityToolkit.Mvvm.WeakReferenceMessenger` (not direct references):
  - `NewImageOpenedMessage(List<Rectangle> Faces)` — FileManager → LabelManager
  - `MetadataLoadedMessage(AutoNumMetaData_V1)` — ImageVM → LabelManager, NameManager, TitleManager
  - `LabelsChangedMessage` — LabelManager → NameManager
- `BaseViewModel` is hand-rolled (not the toolkit's `ObservableObject`). CommunityToolkit.Mvvm is used **only** for the messenger.

### Dependency Flow (no god-object)
```
MainVM  ──creates──►  ImageVM          (standalone, no parent reference)
        ──creates──►  LabelManager     (injected: ImageVM)
        ──creates──►  NameManager      (injected: ImageVM)
        ──creates──►  TitleManager     (no dependencies)
        ──creates──►  FileManager      (injected: MainVM — for dialog coordination only)
```
- **FileManager** retains a `MainVM` reference solely because MahApps `IDialogCoordinator.ShowMessageAsync` requires the window's DataContext as a context object.
- The save path (`ToNumberedBitmap`, `AddMetadata`, `AutoNumMetaData_V1`) receives `LabelManager`, `NameManager`, `TitleManager` as explicit parameters — no navigation through `model.Parent`.

### Shared Style Objects (not static fields)
- `LabelStyle` — owned as `MarkerLabel.Style` (static property), holds Diameter, FontSize, FontColor, EdgeColor, BackgroundColor.
- `TextStyle` — owned as `TextLabel.Style` (static property), holds FontSize, FontColor.
- Each `MarkerLabel`/`TextLabel` instance delegates style properties to the shared style and subscribes via `PropertyChangedEventManager` (weak references — no memory leaks).
- Managers set style via `MarkerLabel.Style.FontColor = value`, not via former scattered static fields.

## Data Flow

### Opening a new image
1. `FileManager.ExecuteOpenImage` → loads `Bitmap`, applies EXIF orientation, checks for AutoNum metadata
2. **No metadata**: detects faces → sets `ImageVM.Bitmap` → calls `Init()` → sends `NewImageOpenedMessage(faces)`
3. `LabelManager` receives message → resets MarkerLabel style defaults → calls `SetLabels(faces)` → adds `Person` objects → calls `Numerate()`
4. `Numerate()` assigns row-sorted numbers → sends `LabelsChangedMessage`
5. `NameManager` receives → refreshes sorted view → repositions name labels

### Opening a previously saved AutoNum image
1. `FileManager` reads EXIF metadata → loads the **original** (un-numbered) image
2. `ImageVM.InitFromMetadata(md)` → restores dimensions, zoom, persons → sends `MetadataLoadedMessage(md)`
3. Each manager receives and applies its styling from metadata (colors, fonts, diameter, title, names)

### Saving
1. `FileManager.ExecuteSaveImage` → calls `ImageVM.ToNumberedBitmap(lm, nm, tm)` (extension method)
2. Renders labels, names, title onto a new composite bitmap
3. Embeds `AutoNumMetaData_V1` as JSON in EXIF UserComment tag → saves as JPEG

## Key External Dependencies
- **Emgu.CV** 4.10 — OpenCV wrapper for face detection (Haar cascade classifier, lazily cached)
- **MahApps.Metro** 3.0-alpha — Metro-style WPF shell, `IDialogCoordinator` for async dialogs
- **CommunityToolkit.Mvvm** 8.4 — `WeakReferenceMessenger` only (not ObservableObject/source generators)

## Coding Conventions
- C# 12 / .NET 8 features; file-scoped namespaces preferred
- PascalCase public members; camelCase private fields
- `is null` / `is not null` over `==`/`!=`
- `nameof(...)` over string literals
- No business logic in code-behind; async/await for I/O
- Release notes go to `docs/release/NEXT_RELEASE.md` during development
