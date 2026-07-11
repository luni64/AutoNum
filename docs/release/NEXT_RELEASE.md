# Next Release

## Features

- **Hovering a name in the Namensliste highlights its label on the image**
  Moving the mouse over a row in the name list now shows a thick magenta ring (sized relative to the label diameter) around the corresponding numbered label, making it easy to match a name back to a face on group photos. The highlight clears as soon as the mouse leaves the row; tracked via hit-testing on mouse move rather than per-row hover events so fast mouse movement can't leave a stale highlight behind.

- **Datei menu replaces the old Öffnen/Speichern buttons and Settings gear**
  The left panel is gone; all file actions now live in a standard Datei menu: Öffnen, Speichern (writes back in place, disabled until the file is no longer the protected original), Speichern unter... (a single dialog offering both JPG and PDF — the format is decided by the extension you pick, not by which filter is highlighted; a new Einstellungen → Export → "Standardformat" setting controls which filter is preselected), and Metadaten exportieren... (on-demand CSV/JSON export via its own dialog, fully independent of the auto-export-alongside-save toggles). Settings moved from a title-bar gear icon into Datei → Einstellungen.... The freed-up left column gives the image preview more room. A new Hilfe menu links to the online manual. Speichern also has a **Strg+S** shortcut.

- **Öffnen remembers the source folder, independent of where you last saved**
  Batch-numbering a folder full of photos used to mean browsing back to the source folder by hand after every save, since the Öffnen dialog inherited whatever folder Speichern unter... (or a configured output folder) last navigated to. Öffnen now always defaults to the folder the current photo was originally opened from, so it stays put across a whole batch regardless of where saves go.

- **Number labels on fresh images are sized from detected face size again**
  With the old Haar-cascade detector, face rectangles were unreliable enough that automatic face-based label sizing was built but never actually enabled — every fresh image was sized purely from image width, regardless of how many people were in it or how large their faces were. Now that YuNet detection is much more consistent, label size is computed from the average diagonal of the detected faces instead, tuned against real photos to look right on both large group shots and close-ups with only a few people: base diameter is 38% of the average face diagonal, capped at 4.5% of the image's own diagonal so a close-up with a couple of large faces doesn't produce oversized labels. Falls back to the previous image-width-based sizing when no faces are detected or face detection is disabled (fixed a related gap where disabling detection left the label size stale from whatever photo was open before).

- **Namensliste/Title/Description/Image-ID font size no longer tied to the label size**
  These previously shared the exact same 100% baseline as the label numbers, so the face-based label sizing above (and its label-circle-fit constraint) indirectly capped how large this text could get too. They're now sized independently from the image's own diagonal instead, with no such ceiling. Persisted per-image the same way label sizing already was, so reopening a saved file restores the exact size in effect when it was saved, even after further retuning.

- **Face-relative label anchor**
  New 3x3-grid control lets you choose where a freshly detected face's label is centered (e.g. top-left, center, bottom-right) instead of always below the chin. Applies only to newly created labels (open/redetect/rotate); existing labels are never moved. Available in two places with different scope: Einstellungen → Erkennung sets the app-wide default applied to every freshly opened image (no redetect button there — it's a pure default, like every other setting); the label formatting dialog (Formatieren, next to the number labels) has its own copy of the grid plus a "Neu Erkennen" button that redetects only the current image with the chosen anchor (same delete-names confirmation as the existing redetect action) without touching the default — "Als Standard übernehmen" in that dialog is what promotes the current choice to the app-wide default. Labels are also no longer placed exactly on the tight detected-face box; every anchor now sits pushed outward by a tunable margin so, for example, "unten Mitte" (bottom center) lands below the chin instead of on it.

- **Ctrl+drag to move all labels together**
  Holding Ctrl while dragging a label in the preview now moves every other label by the same amount, for quick bulk repositioning after zoom/rotation.

- **Much smarter automatic row detection**
  The old row detection sliced the image into equal-height horizontal bands (guessing the row count from the label size), which fell apart on real group photos: unevenly spaced rows, tilted camera, children between adults, and seated vs. standing rows produced far too many rows with boundaries cutting through the middle of them. The new algorithm (`Model/RowClusterer.cs`) grows rows locally out of neighbouring labels (the way OCR segments text lines), merges what no straight boundary could separate anyway, and fits **slanted, parallel row boundaries** that follow the photo's tilt — the boundary lines you see in row mode now start out matching the actual rows. On the test photos this cut detected row counts from 7–9 nonsense rows down to the 3–4 rows a person would actually count. Rows are still resolved from the boundaries, so dragging a label across a boundary or editing boundaries in row mode fine-tunes the result exactly as before.

- **Einstellungen dialog now has OK/Abbrechen**
  The settings dialog previously applied and persisted every change immediately (a slider drag wrote settings.json dozens of times per second) and only offered "Schliessen". It is now transactional: OK saves everything once, Abbrechen (or ✕/Esc) discards all changes made in the dialog. The "Anwenden" buttons still apply the currently edited values to the open image directly. "Als Standard übernehmen" in the formatting dialogs saves immediately, as before.

- **Warning instead of silent fallback when the output folder is unusable**
  If the configured absolute output folder no longer exists (or a relative subfolder can't be created), the Save dialog used to silently fall back to the image's folder. A message now explains what happened and where to fix it.

- **Face detection switched from Haar cascade to YuNet (DNN)**
  Replaced the old `haarcascade_frontalface_default.xml` classifier with OpenCV's YuNet face detector (`FaceDetectorYN`), a small ONNX model that detects faces far more reliably on old/scanned genealogy photos — non-frontal poses, small or blurry faces, and uneven lighting. The old "Empfindlichkeit (ScaleFactor)" / "Bestätigungen (MinNeighbors)" sliders in Einstellungen → Erkennung are gone; the new detector's default confidence threshold works well enough that it isn't user-configurable.

## Bug Fixes

- **Opening and rotating large photos is noticeably faster**
  The preview conversion round-tripped the whole photo through a PNG encode/decode (seconds of pointless compression on big scans); it now copies the pixels directly (15x faster even on medium images, pixel-identical output).

- **Saving large scans no longer briefly allocates huge amounts of memory**
  The JPG/PDF export drew the number labels into a 3x-supersampled copy of the *entire* image (9x the photo's pixel count — ~700 MB for a 20 MP scan, an out-of-memory risk). Labels are now supersampled individually in a small tile and scaled into place, with identical visual quality.

- **Smoother hovering and row-boundary dragging on photos with many people**
  Hover highlights and row-preview coloring used to trigger a full re-sort and re-layout of the names table for every affected person on every mouse move; the names table now only recomputes when something layout-relevant actually changed (row, number, name text).

- **Saving with no image loaded now says so**
  Speichern/Speichern unter with no renderable image silently did nothing; now an error dialog explains it.

- **Save As no longer re-appends the relative output folder on every save**
  With a relative output folder configured (e.g. `autonum`), the first save of a fresh image correctly suggests a subfolder next to the original — but every further "Speichern unter..." resolved the setting again relative to the *current* file and suggested `autonum\autonum\...`, one level deeper each time. The output-folder redirection now applies only to the first save of a fresh image; afterwards Save As suggests the folder the numbered file is already in.

- **Errors while opening an image are no longer silently swallowed**
  A leftover catch from the old Haar-cascade detector ("no faces found" used to arrive as an exception) discarded any `InvalidOperationException` thrown anywhere in the open pipeline — metadata parsing, patch restore, PDF import — with no message to the user. Such failures now surface through the normal error dialog.

- **Window title version now comes from the project file**
  The version shown in the title bar was hardcoded in code; it now reads the `<Version>` property from `AutoNumber.csproj`, so a release bump can't miss it. (Internal: a code-review cleanup pass also removed dead wizard-era views, unused converters/commands, and renamed mismatched files — see `docs/CODE_REVIEW_2026-07-11.md`.)

- **Grayscale JPEGs crashed face detection**
  A grayscale JPEG (single color component) loads via GDI+ as an 8bpp-indexed bitmap, which `Bitmap.ToMat()` converts to a 4-channel (BGRA) `Mat` instead of the expected 3-channel BGR — `FaceDetectorYN.Detect` requires exactly 3 and threw `OpenCV: Number of input channels should be multiple of 3 but got 4`. `FaceDetector` now normalizes any non-3-channel Mat to BGR via `CvtColor` before detecting; normal color JPEGs are unaffected (already 3-channel, no conversion runs).

- **Custom output folder now actually supports relative paths**
  `Einstellungen → Export → Eigenen Ausgabeordner` previously only worked for absolute paths that already existed on disk; a relative value like `AutoNum` silently did nothing, because it was checked against the app's working directory instead of the photo's own folder. It's now resolved as a subfolder next to the source image and created automatically if missing. Also fixed a crash (`ArgumentException` from the native Save dialog) when the relative folder had more than one path segment (e.g. `AutoNum/test`), caused by a stray forward slash reaching `SaveFileDialog.InitialDirectory`.

- **EXIF save: build-breaking typo in `BitmapExtensions.cs`**
  A stray pasted token after `propItem.Len = jsonBytes.Length;` and a misspelled `GetExecutinsgAssembly()` call broke compilation. Fixed both.

- **Saved label position was off-center by half the label diameter**
  `Label.CenterX/CenterY` is now always the label circle's true center (canvas `X`/`Y` are derived from it), rather than being treated inconsistently as a top-left corner in some places. Metadata bumped to V5 as a version marker; V1-V4 files are migrated on load so previously saved images/PDFs still open in the same visual position.

- **Switching the face-anchor radio buttons could snap back to the previous selection**
  `EnumBooleanConverter.ConvertBack` now ignores the `false` notification a `RadioButton` fires for the option that just got unchecked, so it no longer immediately resets the bound enum.

- **PDF: image vanishes on scroll in Acrobat DC at 100% zoom**
  The image embedded in the PDF was saved as JPEG (`DCTDecode` + `ICCBased` colorspace + `/ColorTransform 0`), a combination that caused Acrobat DC's tile cache to drop the image on re-render at 100% zoom. Fixed by embedding the image as PNG instead, which has no colour-transform ambiguity.

- **PDF: `/PageMode /UseNone` incremental update wrote an invalid xref entry**
  The xref entry was 21 bytes instead of the PDF-spec-required 20 (a trailing space before `\r\n` was one byte too many). This made `AppendPageModeUseNone` write a structurally invalid xref table. Fixed by removing the extraneous space.
