# Next Release

## PDF Export / Import

- Switched editable PDF payload embedding from a custom appended byte footer to standard PDF file attachments.
- Embedded payload is now stored as `autonum-payload.zip` attachment (`autonum-data`) for better viewer compatibility.
- PDF import now extracts embedded payload attachments via `PdfPig` instead of parsing custom trailing bytes.
- Simplified editable payload content to `metadata + base image` (removed composite-and-patch payload path for PDFs).

## PDF Metadata

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

## Notes

- This release improves compatibility with Acrobat Reader and keeps AutoNum round-trip editing data in a standards-compliant attachment model.
