# Next Release

## Bug Fixes

- **PDF: image vanishes on scroll in Acrobat DC at 100% zoom**
  The image embedded in the PDF was saved as JPEG (`DCTDecode` + `ICCBased` colorspace + `/ColorTransform 0`), a combination that caused Acrobat DC's tile cache to drop the image on re-render at 100% zoom. Fixed by embedding the image as PNG instead, which has no colour-transform ambiguity.

- **PDF: `/PageMode /UseNone` incremental update wrote an invalid xref entry**
  The xref entry was 21 bytes instead of the PDF-spec-required 20 (a trailing space before `\r\n` was one byte too many). This made `AppendPageModeUseNone` write a structurally invalid xref table. Fixed by removing the extraneous space.
