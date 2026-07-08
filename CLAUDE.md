# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

AutoNum (product name "AutoNumber") is a Windows WPF desktop app (.NET 8, C# 12) for genealogists. It opens a photo, detects faces via OpenCV (Emgu.CV), places numbered markers on them, lets the user attach a name list/title/metadata, and exports a flattened JPG or an editable PDF. Single project, no test suite exists in this repo.

**Read `docs/ARCHITECTURE.md` first** — it is the authoritative architecture reference (project structure, design patterns, data flow for open/save/rotate, the scale-factor sizing model, and the slider↔scale mapping). Keep it in sync when architecture changes.

## Commands

```
dotnet build AutoNum.sln -c Debug          # build
dotnet build AutoNum.sln -c Release        # build release
dotnet run --project AutoNum\AutoNumber.csproj
dotnet publish AutoNum\AutoNumber.csproj -c Release   # produces build for the installer
```

There is no automated test project — verification is manual (run the app and exercise the UI flow).

Building the installer (after a Release publish): open `installer\setup.iss` in Inno Setup, or `ISCC.exe installer\setup.iss`. Requires Inno Setup 6 plus `installer\CodeDependencies.iss` (from InnoDependencyInstaller) which is not checked in. See `installer/README.md`. Bump the version in `installer\setup.iss` before building.

## Repository layout gotcha

The actual project lives under `AutoNum/` (e.g. `AutoNum/ViewModels`, `AutoNum/Views`, `AutoNum/AutoNumber.csproj`). There are also stray top-level `ViewModels/` and `Views/` directories at the repo root containing a handful of stale, tracked files (e.g. an empty `ViewModels/LabelManager.cs`) — these are leftovers, not part of the build. Always work under `AutoNum/`.

## Architecture summary

(Full detail in `docs/ARCHITECTURE.md`; keep both in sync.)

- **MVVM + Messenger**: Views bind only to ViewModels. Cross-VM communication uses `CommunityToolkit.Mvvm.WeakReferenceMessenger` (`NewImageOpenedMessage`, `MetadataLoadedMessage`, `LabelsChangedMessage`).
- **`MainVM` is the composition root**, owning `ImageVM`, `FileManager`, `LabelManager`, `NameManager`, `TitleManager`, `ImageInfoManager`, `ImageIdManager`, `SettingsManager`.
- **Scale-factor sizing model**: every text/label manager holds a `FontScale`/`LabelScale` (0.25–4.0, 1.0 = unscaled baseline) plus a computed base size; `ResolveSize(base, scale) = base * scale`. UI sliders map to scale exponentially (`scale = 0.25 * 16^sliderPos`), not linearly.
- **Metadata versioning**: `AutoNumMetaData` routes between V1–V4 JSON schemas embedded in JPEG EXIF `UserComment` or in a PDF-embedded payload zip (`PdfPayloadStore`). V3+ stores exact sizing anchors and relative scales so reopening is deterministic; V1/V2 migrate via legacy size ratios.
- **Patch-based restore**: saved JPEGs/PDFs embed pixel patches (regions hidden under labels) via `AppSegmentIO`/JPEG APP4 segments (JPG) or payload patches (PDF), so reopening doesn't require the original source file (see `docs/PATCH_RESTORE_PLAN.md`).
- **Three renderers must stay visually consistent**: WPF/XAML live preview (`Marker.xaml`), GDI+ JPG export (`ExtensionMethods`, supersampled anti-aliased label drawing), and QuestPDF PDF export (`FileManager.WritePdf`). Names-table geometry is computed once in `NameTableLayoutEngine`/`NamesTableLayout` and shared across all three to avoid drift.
- **Settings**: app-wide defaults persist to `%AppData%/AutoNum/settings.json` via `SettingsManager`; they seed new/fresh-image sessions and detector defaults but never override per-image values restored from metadata.

## Coding conventions

- C# 12 / .NET 8 features; file-scoped namespaces; single-line usings.
- Pattern matching, switch expressions, expression-bodied members where natural.
- `nameof(...)` instead of string literals for member names.
- PascalCase for public types/members, camelCase for private fields/locals/methods; interfaces prefixed `I`.
- Respect nullable annotations; use `is null` / `is not null`.
- Follow MVVM strictly — no business logic in code-behind.
- Use `ObservableCollection<T>` / `INotifyPropertyChanged` for bindings; prefer async/await for hardware/I/O, never block the UI thread.
- Persist only model-level scale values in settings/model code — never persist view-specific slider positions (slider↔scale mapping is a UI implementation detail that can change).

## Working conventions

- When asked to "review" code, report findings only — do not make changes unless explicitly asked.
- Never commit, stage, or push without an explicit user request.
- Prefer direct file-edit tools over terminal-based file writing.
- Git/terminal: use `--no-pager` for `git diff`/`log`/`show`. For multi-line commit messages, write to a temp file and use `git --no-pager commit -F <file>` rather than inline here-strings.

## Release docs

- `docs/release/NEXT_RELEASE.md` is the scratchpad for the unreleased version — add bug-fix/improvement notes here as you go.
- `docs/release/RELEASE_NOTES_DE.md` holds the German user-facing notes for the current/most recent release.
- Workflow: after tagging a release, roll `NEXT_RELEASE.md` entries into the release notes and reset `NEXT_RELEASE.md`.
