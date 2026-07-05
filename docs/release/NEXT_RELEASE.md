# Next Release

## Row Definition & Numbering Workflow

- Added row-definition workflow with visual boundary editor for assigning labels to rows.
- Added row dividers in the names list and persisted row boundary metadata for later adjustment.
- Fixed row assignment on oblique boundaries by using the actual label X coordinate during interpolation.
- Improved row architecture by removing duplicated row-resolution logic and reducing view/model coupling.
- Removed obsolete row-definition window code and orphaned command members after consolidation.

## UI Improvements

- Added a "Zoom to image" action that includes row controls in the fit area while row mode is active.
- Simplified row controls UI by removing the row count text field.
- Moved the row mode button into the numbering group for a cleaner workflow.
- Replaced BoxIcons/JamIcons with Material icons and removed unused icon packages.

## PDF Export / Import

- Switched editable PDF payload embedding from a custom appended byte footer to standard PDF file attachments.
- Embedded payload is now stored as `autonum-payload.zip` attachment (`autonum-data`) for better viewer compatibility.
- PDF import now extracts embedded payload attachments via `PdfPig` instead of parsing custom trailing bytes.
- Simplified editable payload content to `metadata + base image` (removed composite-and-patch payload path for PDFs).
- Added standard PDF document metadata on export:
  - Title
  - Author
  - Subject
  - Keywords
  - Creator
  - Producer
  - Language
  - CreationDate
  - ModifiedDate

## JPEG Reopen Stability

- Fixed stale V2/V3 metadata synchronization before save by recomputing reconstruction-critical fields (`OriginalImageWidth`, `OriginalImageHeight`, `TitleHeight`) and V3 sizing anchors/scales from current runtime state.
- This resolves incorrect crop offset/region when reopening previously saved `_num.jpg` files that reconstruct the base image from APP4 patches.

## Notes

- Focus of this release is row-editing usability, robust round-trip import/export, and UI cleanup.
