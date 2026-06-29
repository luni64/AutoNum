# Export Quality + Editable PDF Payload Plan

## Goal
Improve output quality and round-trip editing while keeping the current WPF vector preview model.

## Scope
1. Improve label quality in exported JPG files (especially low-resolution/small-face scenarios).
2. Align names-table design between bitmap preview/JPG and PDF export.
3. Preserve current self-contained JPG editability and add equivalent self-contained PDF editability.

## Non-Goals
- Do **not** switch main-window preview to PDF.
- Do **not** remove or degrade current `_num.jpg` EXIF+APP4 restore behavior.

## Current Architecture Constraints (from repo docs)
- App is WPF/.NET 8/C# 12 with MVVM.
- On-screen preview uses WPF overlays (vector quality) and should stay that way.
- JPG round-trip editing currently relies on:
  - EXIF UserComment metadata (`AutoNumMetaData_V*` JSON)
  - APP4 patch segments (overwritten pixels)
- PDF export is currently sidecar-style output (QuestPDF) without edit payload restore path.

## Implementation Plan

### Step 1 — Define PDF payload contract (round-trip edit model)
Create a versioned payload format for PDF that can restore full editable state without external original files.

Payload must include:
- Metadata JSON (`AutoNumMetaData_V3` or later)
- Pixel restoration data strategy:
  - patches (preferred for size), and/or
  - optional embedded original image bytes (fallback mode)
- Format/version marker and integrity fields (e.g., schema version, created timestamp, checksum)

Acceptance:
- Contract is documented in code and can evolve version-safely.

---

### Step 2 — Implement PDF payload I/O component
Add a dedicated component for:
- serializing payload to zip bytes,
- embedding/extracting payload in/from PDF container.

Expected responsibilities:
- `CreatePayloadZip(...)`
- `ReadPayloadZip(...)`
- `EmbedPayload(pdfBytes, payloadZipBytes)`
- `TryExtractPayload(pdfBytes, out payloadZipBytes)`

Notes:
- Embedded payload is non-rendered document data.
- Handle missing/invalid payload gracefully.

Acceptance:
- Unit/integration-level smoke coverage or manual verification with sample files.

---

### Step 3 — Extend PDF export to embed AutoNum payload
In the existing PDF export flow (`FileManager.WritePdf` path):
- keep visible PDF rendering as-is,
- add payload embedding step so `_num.pdf` becomes self-contained for editing.

Acceptance:
- Exported PDF opens normally in standard viewers.
- Payload is present and extractable by app.

---

### Step 4 — Extend open-file flow to support editable `_num.pdf`
In `FileManager.ExecuteOpenImage` open path:
- detect PDF input,
- extract payload,
- rebuild editable bitmap state and metadata,
- initialize ViewModels/managers via existing restore logic.

Keep existing JPG flows intact:
- V1 original-file behavior,
- V2/V3 patch-based self-contained restore.

Acceptance:
- User can open exported `_num.pdf` and continue editing without external original image.

---

### Step 5 — Improve JPG label quality in export rasterization
Apply export-only rendering quality improvements in bitmap pipeline (`ExtensionMethods`):
- supersampled label rendering (e.g., 2x or 4x offscreen),
- anti-aliased vector/text settings,
- high-quality downscaling to final image size.

Important:
- Do **not** enforce a minimum label diameter (would occlude faces).
- Do **not** change preview behavior.

Acceptance:
- Label edges and number glyphs look visibly cleaner on low-res/small-face photos.

---

### Step 6 — Introduce shared names-table layout model
Create shared table layout/style model used by both:
- bitmap preview/JPG footer names rendering,
- PDF names table rendering.

Shared properties should include:
- columns/width rules,
- header style,
- border/padding,
- row spacing and alignment.

Acceptance:
- JPG/preview and PDF names list appear stylistically consistent.

---

### Step 7 — Validate round-trip and quality scenarios
Manual validation matrix:
1. Open/edit/save `_num.jpg` (existing behavior unchanged).
2. Open/edit/save `_num.pdf` (new behavior).
3. Export low-resolution sample JPG and compare label quality before/after.
4. Compare names-table visual parity across JPG/preview/PDF.

Acceptance:
- No regression in JPG metadata/patch restore.
- PDF round-trip editability works.
- Visual quality improvements confirmed.

---

### Step 8 — Update release notes scratchpad
Add concise entries to:
- `docs/release/NEXT_RELEASE.md`

Include:
- JPG export label quality improvement,
- shared names-table styling parity,
- editable PDF payload support.

Acceptance:
- Release scratchpad reflects implemented work.

## Risks / Caveats
- Some external PDF processing tools may strip attachments/metadata.
- PDF library capabilities for embedded file handling may require adaptation.
- Cross-renderer typography may still need small tolerance tuning.

## Thread Recovery Instructions
If a chat thread crashes, start a new thread and paste this:

"Continue implementation from `docs/implementation/EXPORT_PDF_PLAN.md` in my workspace. Read `.github/copilot-instructions.md` and `docs/ARCHITECTURE.md` first. Then execute the plan steps in order, with minimal code changes, and validate build/tests before finishing."

## Status Snapshot
- Plan authored and persisted.
- Implementation not started yet in this file-backed workflow.
