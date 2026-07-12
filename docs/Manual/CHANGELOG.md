# Changelog

All notable changes to this project are documented in this file. The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions correspond to tagged [GitHub releases](https://github.com/luni64/AutoNum/releases).

## [Unreleased]

See [NEXT_RELEASE.md](https://github.com/luni64/AutoNum/blob/main/docs/release/NEXT_RELEASE.md) for changes not yet published in a release.

## [2.2.0] - 2026-07-06

### Added
- Automatic row detection and management: rows can now be detected automatically, edited manually, and reset flexibly.
- New "Zoom to Image" button that fits both the image and the row controls into view together while in row mode.
- New "Detection" section in Settings for face detection and row detection.
- Face detection and row detection can each be enabled/disabled independently for new images.
- Detection parameters can be adjusted directly in Settings and reapplied with one click, with a reset-to-default option.
- Number formatting can now be saved as a default.

### Changed
- The name list and row-editing UI were visually reworked and are easier to use.
- The row-mode controls were simplified: the row-count input field was removed, and the row-mode toggle now lives in the "Numbering" group.
- The default layout for new images was reworked and is now more compact.
- Editable PDF files are now saved as standards-compliant PDF attachments and can be reopened.
- Many toolbar icons were replaced with a more consistent icon set.

### Fixed
- AutoNum now shows a retry/cancel prompt when a file being saved is already open in another program.
- The visible image region is now correctly restored when reopening a previously saved image.
- Assigning people to rows now works correctly even when row boundaries are dragged at an angle.
- The PDF name list now uses a narrower number column.
- Long title and image-information texts now wrap instead of being cut off.

## [2.1.0] - 2026-07-01

### Added
- New "Export Now" button under Settings → Export for direct metadata export.

### Fixed
- PDF export now respects the metadata options under Settings → Export (CSV/JSON sidecar files), matching JPG export.
- Previously entered image-related texts (title, information, image ID) are now correctly reset when opening a new image.

## [2.0.0] - 2026-06-30

### Added
- New image action bar below the preview: rotate 90°, fit zoom to content.
- Separate save buttons: "Save as JPG" and "Save as PDF" (remains editable).
- Metadata export: title, description, ID, and name list can be exported as `.csv` or `.json` for further processing in Excel or databases.
- Reopen PDFs: PDF files created by AutoNumber can be reopened and edited later, like JPGs.
- More control over the name list: number of columns per image is configurable (1–4), e.g. for compact lists on larger group photos.
- Dedicated fields for image information and image ID, separate from the title, each independently shown/hidden and styled.

### Changed
- Unified formatting dialog for title, image information, image ID, and name list, with clearer percentage display for sizes.
- Reworked Settings window with clearer sections for fonts, detection, and saving; defaults are easier to apply.
- More reliable restoration of sizes, fonts, and visibility when reopening a file.
- Improved rendering quality for small numbers and name lists in JPG and PDF export.
- AutoNum files no longer require the original image to edit — all necessary information is stored directly in the metadata.

## [1.3.0] - 2025-10-25

### Fixed
- Label size was set incorrectly (too small) when opening images created with AutoNum.
- Opening images created with AutoNum triggered an automatic renumbering, altering manually adjusted numbering.

## [1.2.1] - 2025-03-10

### Added
- Ability to search for the original image when opening a numbered image whose original can't be found automatically.

### Changed
- Switched to `Bitmap.Open` instead of the OpenCV open method, reducing supported input formats to JPG, PNG, TIFF, BMP, GIF, and EXIF.
- Export temporarily restricted to JPG (due to metadata handling).

### Fixed
- Label size was wrong when opening a numbered image.
- Fixed crashes when opening non-image files.

## [1.2.0] - 2025-03-05

### Added
- Beta support for opening and editing previously numbered images, e.g. to add identified person names to an existing image.

> **Note:** AutoNumber requires the original image to edit a numbered one; its location is stored in the metadata on save. Images created with older versions that didn't store this metadata cannot be edited.

## [1.1.15] - 2025-03-03

### Changed
- New GUI and new features (first tracked release).
