# AutoNum Codebase Review — 2026-07-11

Full-codebase review ahead of the next release: architectural quirks, MVVM compliance, dead code, duplication, performance. Findings only — nothing has been changed yet. Add comments/decisions inline (e.g. `> LN: ...`).

## Overall verdict

The architecture is in good shape: the MVVM + Messenger design described in `ARCHITECTURE.md` is genuinely followed, the versioned-metadata and three-renderer models are clean, and recent code (RowClusterer, PdfPayloadStore, NameTableLayoutEngine, the sizing model) is well-documented and disciplined. The debt is concentrated in three places: **dead wizard-era leftovers**, **one ViewModel doing View work (`FileManager`)**, and **copy-paste multiplication** (settings, text managers, row-boundary math). All of it is fixable incrementally — nothing requires a big rewrite before the release.

## 1. Dead code (safe deletions, biggest presentability win)

- [x] **`Views/WizardViews/LabelWiz.xaml` and `AnalyzeImageWiz.xaml`** — excluded from the build in the csproj (`Page Remove`) and only referenced by commented-out lines in `MainWindow.xaml:32-33`. Pure fossils.
- [x] **`Views/WizardViews/GeneralSettingsView.xaml`** — compiled, but referenced nowhere.
- [x] **`Views/S1_SelectFile.xaml/.cs`** — old wizard step 1, referenced nowhere.
- [x] **`BaseViewmodel.cs` → `AsyncCommand`** — zero usages, and its `CanExecute` is actually buggy (`!(_isExecuting && _canExecute())` returns *true* while executing when `_canExecute()` is false). Deleting it removes a latent bug.
- [x] **`BaseViewModel.SetProperty<T,TProperty>(obj, expression, …)`** — the reflection/expression overload has zero callers.
- [x] **Five converters in `Converters.cs`**: `BoolToObjectConverter`, `IntToMarginConverter`, `NullToEnabledConverter`, `NullToVisibilityConverter`, `ComparisonConverter` — no XAML usage. `StringToImageConverter` is declared as a resource in `MainWindow.xaml:39` but never consumed → delete both class and resource.
- [x] **`MarkerLabel.Name`** property (`MarkerRect.cs:66`) — never read or written.
- [x] **`Analyzer.PlaceTitle` and `Analyzer.GetLargestItem`** — no callers. Removing `PlaceTitle` also removes one of Analyzer's two ViewModel dependencies.

## 2. Correctness findings

- [x] **`FileManager.ExecuteOpenImage` swallows any `InvalidOperationException`** (`FileManager.cs:133`): `catch (InvalidOperationException) { Trace.WriteLine("No faces found"); }` covers the *entire* open pipeline — metadata parsing, patch restore, PDF import. A real failure in any of those shows the user nothing at all. This catch predates the YuNet detector (which no longer throws for "no faces") and should probably be deleted outright, letting the generic error dialog below it handle everything.
- [x] **`WriteJpg`/`WritePdfWithSidecars` silently no-op** when `ToNumberedBitmap` returns null — user clicks Speichern, nothing happens, no message. Rare (null bitmap), but a dialog would be better.
- [x] **Version string `"AutoNumber V2.3.0"` is hardcoded twice** in `MainVM.Title`, separately from the installer's version in `setup.iss`. Every release bump touches multiple places by hand; read it from the assembly version instead (single source in the csproj).

## 3. MVVM compliance

Mostly clean — views bind to VMs, cross-VM communication uses the messenger, business logic lives in VMs/Model. The violations, worst first:

- [x] **`FileManager.ShowRetryCancelDialog` builds a WPF `Window` with Grid/TextBlock/Buttons inside a ViewModel** (`FileManager.cs:366-431`) — the clearest MVVM break in the codebase (note `using System.Windows.Controls` in a VM). It should be a method on `IDialogService` (which exists precisely for this).
- [x] **~200 lines of QuestPDF document layout inline in `FileManager.WritePdf`** — rendering belongs in the Model layer next to the other renderers (e.g. `Model/PdfReportRenderer`). This would shrink FileManager from 863 lines to something presentable and complete the "three renderers" story architecturally.
- [ ] **`ZoomBorder.ZoomBorder_PreviewMouseRightButtonDown`** adds/deletes persons, resolves rows and renumbers directly in code-behind. The hit-testing is legitimately view work; the "add person at point / delete person" logic should be VM commands the view invokes.
- [ ] **Model → ViewModel dependencies**: `Analyzer`, `ExtensionMethods` and `AutoNumMetaData_V4` all reach *up* into `AutoNumber.ViewModels` (`Person`, `TextLabel`, the five managers, `RowBoundary`). The cheapest structural fix with the most effect: **move `RowBoundary` (and arguably `MarkerLabel`/`TextLabel`/`Person`'s data parts) into Model** — `RowBoundary` is persisted metadata and has no view behavior; it simply lives in the wrong namespace.
- [ ] **`MainVM.DialogCoordinator` setter injects `LabelManager._mainVM` and `_dialogCoordinator` as internal fields** — temporal coupling that only works because the window sets the coordinator right after construction. Constructor injection (or an `Initialize` call in `MainVM`'s ctor) would remove the trap.
- [ ] **Accepted quirks worth keeping but documenting**: the static shared `MarkerLabel.Style`/`TextLabel.Style` (global mutable state — fine for a single-window app, fatal if you ever want two documents open), and `PictureDisplay`'s hand-rolled marker management with programmatic bindings instead of an `ItemsControl` + `DataTemplate`s (~150 lines that WPF could do declaratively — a worthwhile refactor, but the riskiest one on this list; don't do it right before a release).

## 4. Duplication / over-complication

- [x] **The row-boundary midline computation exists three times** — `LabelManager.CalculateAndStoreRowBoundaries`, `ImageVM.CalculateBoundariesFromDetectedRows`, `RowDefinitionManager.TryInitializeSessionFromDetectedRows` — byte-for-byte the same grouping + `(MaxY+MinY)/2` + clamp loop. And **the "resolve row from boundaries" loop exists five times** (`ImageVM:257`, `RowDefinitionManager:157`, `RowDefinitionSession.ResolveRow`, `PictureDisplay.ResolvePreviewRow`, `RowClusterer`). One static helper class (e.g. `Model/RowBoundaryMath`) would collapse all eight sites and make the row system much easier to reason about.
- [ ] **Four near-identical text managers**: `TitleManager`, `ImageInfoManager`, `ImageIdManager` and (partially) `NameManager` repeat the same `FontScale`-clamp/`ApplyScale`/colors/font-family/metadata-restore/legacy-scale-fallback pattern. A `TextElementManagerBase` would remove ~250 lines and guarantee the four can't drift apart.
- [ ] **`SettingsManager` is 647 lines of five-fold copy-paste** (per-element scale/colors/enabled × Label/Names/Title/ImageInfo/ImageId), and `SaveSettings()` re-copies every property into `AppSettings` by hand. A per-element defaults record (`ElementDefaults { Scale, FontColor, BackgroundColor, Enabled }`) keyed by element would cut it to ~200 lines. Also: **every property setter writes `settings.json` synchronously** — dragging a default-scale slider in the settings dialog produces a file write per tick. Debounce, or save on dialog close.
- [ ] **`OpenFileInfo`/`SaveFileInfo`** are identical twins; `DialogService.ShowDialog(object) → object?` is a weakly-typed switch — fine for now, but typed methods (`ShowOpenDialog`, `ShowSaveDialog`, `ShowError`, plus the new `ShowRetryCancel`) would be clearer and enable the retry-dialog fix in §3.
- [ ] **`FindParentWithDataContext<T>` is implemented three times** (`Marker.xaml.cs`, `ZoomBorder.cs`, plus `PictureDisplay.FindParent`).

## 5. Performance observations

- [x] **`ExtensionMethods.drawLabels` allocates a 3× supersampled overlay of the entire image** (`ExtensionMethods.cs:39`): 9× the pixel count — a 20 MP scan needs a ~700 MB temporary bitmap during every save. Genealogy scans get big; this is an OutOfMemory risk. Supersample per-label tiles (labels cover a tiny fraction of the image) for the same quality at ~1% of the memory.
- [x] **`NameManager.Person_PropertyChanged` reacts to *every* person property** with `Refresh()` + `ShowNames()` (full collection-view re-sort + GDI text measurement of the whole names table). `IsSelected` (hover highlight fires on every mouse move) and `RowPreviewActive`/`RowPreviewColor` (set on *all* persons on *every* row-boundary drag delta) all trigger it. On a 45-person image that's hundreds of full relayouts per second while dragging. Filter to the properties that actually affect layout (`Row`, name text, number).
- [ ] **`BitmapToBitmapSource` re-encodes the full photo as PNG** on every binding refresh — slow for large scans; `Imaging.CreateBitmapSourceFromHBitmap` (with proper handle cleanup) or caching would remove a noticeable open-image delay.

## 6. Naming and style nits

- [x] File/class mismatches: `BaseViewmodel.cs` (class `BaseViewModel`), `MarkerRect.cs` (contains `MarkerLabel`), `TiltleView.xaml` (typo for "Title", referenced from `MainWindow.xaml`).
- [ ] Convention violations: public `drawLabels`/`drawNames` (camelCase) in `ExtensionMethods`, `FileManager.openFromOriginalFile`, `RelayCommand`/`AsyncCommand` nested inside `BaseViewModel` rather than standalone files.
- [x] String literals where `nameof` belongs: `"IsLocked"` (`Marker.xaml.cs:78`), `"FullName"` (`MarkerRect.cs:63`, `TextLabel.cs`).
- [x] Stale header comment "Interaktionslogik für Bookmark.xaml" in `Marker.xaml.cs`.
- [x] `DialogService` shows an English "Error" title in an otherwise German UI (fold into the §3/§4 dialog-service rework).

## 7. Stale documentation (cheap, high presentability value)

- [x] **`CLAUDE.md`**: the "Repository layout gotcha" (stray root-level `ViewModels/`/`Views/`) is obsolete — those files are gone; and `installer/CodeDependencies.iss` *is* checked in now, contradicting the Commands section.
- [x] **`ARCHITECTURE.md`**: still lists `LabelWiz.xaml` as the FontManager's first usage context and includes the dead wizard files in the project tree.

## Proposed improvement plan

**P1 — before the release (low risk, ~a day):** delete all §1 dead code and files; fix the swallowed `InvalidOperationException`; version string from assembly info; rename the three mismatched/typo files; refresh CLAUDE.md/ARCHITECTURE.md. This alone removes most of what a reviewer would trip over.

**P2 — shortly after (contained refactors):** move the retry dialog into `IDialogService` (+ typed dialog methods); extract `PdfReportRenderer` out of `FileManager`; consolidate the row-boundary math into one helper; add the `NameManager` property filter (perf) and the per-label supersampling fix (memory).

**P3 — when convenient (structural):** `TextElementManagerBase`; data-driven `SettingsManager` with debounced saves; move `RowBoundary`/marker data types into Model; optionally the `ItemsControl`-based `PictureDisplay` rewrite (largest payoff in code size, largest regression risk — needs its own careful session).

## Review scope

Read in full: all ViewModels, `PictureDisplay`/`Marker`/`ZoomBorder`/`MainWindow` code-behind, `Analyzer`, `ExtensionMethods`, `AppSettings`, `Converters`, `Messages`, `DialogService`, `BulkObservableCollection`, `RowDefinitionSession` (partial). Skimmed/structurally checked: `BitmapExtensions`, `AppSegmentIO`, `PdfPayloadStore`, `AutoNumMetaData*`, `FontManager`, `TextFormatDialog`, `SettingsWindow`, `FaceAnchorPicker`, remaining WizardView XAML. Usage claims (dead code) verified by repo-wide grep; nothing verified by running the app.
