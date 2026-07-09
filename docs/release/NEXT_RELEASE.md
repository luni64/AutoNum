# Next Release

## Features

- **Face-relative label anchor (Einstellungen → Erkennung)**
  New 3x3-grid control lets you choose where a freshly detected face's label is centered (e.g. top-left, center, bottom-right) instead of always below the chin. Applies only to newly created labels (open/redetect/rotate); existing labels are never moved. A "Neu Erkennen" button sits right beside the control to re-run detection immediately with the new anchor.

- **Ctrl+drag to move all labels together**
  Holding Ctrl while dragging a label in the preview now moves every other label by the same amount, for quick bulk repositioning after zoom/rotation.

- **Face detection switched from Haar cascade to YuNet (DNN)**
  Replaced the old `haarcascade_frontalface_default.xml` classifier with OpenCV's YuNet face detector (`FaceDetectorYN`), a small ONNX model that detects faces far more reliably on old/scanned genealogy photos — non-frontal poses, small or blurry faces, and uneven lighting. The old "Empfindlichkeit (ScaleFactor)" / "Bestätigungen (MinNeighbors)" sliders in Einstellungen → Erkennung are gone; the new detector's default confidence threshold works well enough that it isn't user-configurable.

## Bug Fixes

- **EXIF save: build-breaking typo in `BitmapExtensions.cs`**
  A stray pasted token after `propItem.Len = jsonBytes.Length;` and a misspelled `GetExecutinsgAssembly()` call broke compilation. Fixed both.

- **Saved label position was off-center by half the label diameter**
  `Label.CenterX/CenterY` is now always the label circle's true center (canvas `X`/`Y` are derived from it), rather than being treated inconsistently as a top-left corner in some places. Metadata bumped to V5 as a version marker; V1-V4 files are migrated on load so previously saved images/PDFs still open in the same visual position.

- **Switching the face-anchor radio buttons could snap back to the previous selection**
  `EnumBooleanConverter.ConvertBack` now ignores the `false` notification a `RadioButton` fires for the option that just got unchecked, so it no longer immediately resets the bound enum.

- **Duplicate "Neu Erkennen" button in Einstellungen → Erkennung**
  An earlier reorder of the tab's sections left two copies of the button; removed the extra one.

- **PDF: image vanishes on scroll in Acrobat DC at 100% zoom**
  The image embedded in the PDF was saved as JPEG (`DCTDecode` + `ICCBased` colorspace + `/ColorTransform 0`), a combination that caused Acrobat DC's tile cache to drop the image on re-render at 100% zoom. Fixed by embedding the image as PNG instead, which has no colour-transform ambiguity.

- **PDF: `/PageMode /UseNone` incremental update wrote an invalid xref entry**
  The xref entry was 21 bytes instead of the PDF-spec-required 20 (a trailing space before `\r\n` was one byte too many). This made `AppendPageModeUseNone` write a structurally invalid xref table. Fixed by removing the extraneous space.
