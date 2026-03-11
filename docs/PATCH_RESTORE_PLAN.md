# Self-Contained Image Editing — Patch-Based Restore Plan

## Goal
Remove the dependency on the original image file when re-opening an AutoNum-generated image.
Instead of requiring the original file, embed small pixel patches (the regions hidden under
numbered labels) directly in the saved JPEG. On re-open, paste the patches back to reconstruct
a clean base image for editing.

## Current Flow (V1)

### Save
1. `ToNumberedBitmap()` creates a new canvas (original + title region + footer region)
2. Draws original image at offset `(0, titleHeight)`
3. Draws title bar, name footer, labels, name text overlays
4. `AddMetadata()` writes `AutoNumMetaData_V1` JSON into EXIF `UserComment` (0x9286)
5. `bmp.Save(filename, ImageFormat.Jpeg)`

### Re-open
1. `FileManager` loads the file, finds AutoNum metadata → needs the **original** file
2. Loads original from disk → `InitFromMetadata()` restores labels from JSON positions
3. If original file is missing → prompts user → fails if not found

## New Flow (V2)

### Save — what changes
1. `ToNumberedBitmap()` creates the canvas and draws the original image (same as today)
2. **NEW: Before drawing labels**, capture a rectangular patch for each label:
   - Bounding box: `(label.X, label.Y + titleHeight, Diameter, Diameter)` — the same
     `RectangleF` used by `drawLabels` for `FillEllipse`
   - Capture via `Bitmap.Clone(rect, PixelFormat)` on `bmpFinal`
   - Encode each patch as PNG into a `byte[]`
   - Collect all patches into a `List<PatchData>` where `PatchData = { Index, X, Y, PngBytes }`
3. Draw labels and names (same as today)
4. `AddMetadata()` writes V2 JSON into EXIF `UserComment` (same tag, new version field)
   - V2 JSON adds `OriginalImageHeight` (for cropping title/footer)
   - V2 JSON adds `OriginalImageWidth`
   - V2 JSON **does not** include patch pixel data (too large for EXIF)
5. **NEW: `AppSegmentWriter.WritePatches(jpegBytes, patches)`** embeds patch data into
   a custom **APP4** segment in the JPEG byte stream
   - Magic prefix: `AutoNum\0` (7 bytes)
   - Followed by a simple binary format:
     ```
     [uint16  patchCount]
     For each patch:
       [float32 X] [float32 Y] [float32 W] [float32 H]   -- bounding box (16 bytes)
       [uint32  pngLength]                                  -- length of PNG blob
       [byte[]  pngData]                                    -- raw PNG bytes
     ```
   - If total exceeds 65,533 bytes → chain into multiple APP4 segments
     (each starts with `AutoNum\0` + `uint16 segmentIndex` + `uint16 totalSegments`)
6. Save the final JPEG bytes to disk (replaces `bmp.Save()` since we need to inject APP4
   after GDI+ encodes the JPEG)

### Re-open — what changes
1. `FileManager` loads the file, finds AutoNum metadata (V2)
2. **NEW**: If V2 metadata detected:
   a. Read APP4 segments → `AppSegmentReader.ReadPatches(jpegBytes)` → `List<PatchData>`
   b. Crop the loaded bitmap to `OriginalImageWidth × OriginalImageHeight` starting at
      `(0, titleHeight)` — removes title bar and name footer
   c. Paste each patch back at its `(X, Y)` position → reconstructed clean base image
   d. Proceed with `InitFromMetadata()` as today (positions, styles, names)
3. **Backward compat**: If V1 metadata detected → fall back to current flow (load original file)

## File Changes

### New file: `Model/PatchData.cs`
Simple data class:
```csharp
record PatchData(float X, float Y, float Width, float Height, byte[] PngBytes);
```

### New file: `Model/AppSegmentIO.cs`
Static class with:
- `WriteAutoNumSegments(byte[] jpegBytes, List<PatchData> patches) → byte[]`
  — Inserts APP4 segment(s) after the JPEG SOI marker
- `ReadAutoNumSegments(byte[] jpegBytes) → List<PatchData>?`
  — Scans for APP4 segments with `AutoNum\0` prefix, reassembles if chained
- `HasAutoNumSegments(byte[] jpegBytes) → bool`
  — Quick check without full parse

### Modified: `Model/ExtensionMethods.cs`
`ToNumberedBitmap()`:
- After drawing the original image onto `bmpFinal` but **before** `drawLabels()`:
  capture patches for each label bounding box
- Return type changes from `Bitmap?` to a result containing both the bitmap and
  the patch list (or attach patches to a context object)
- Option: change signature to `ToNumberedBitmap(..., out List<PatchData> patches)`
  or return a wrapper record

### Modified: `Model/AutoNumMetaData.cs`
- New class `AutoNumMetaData_V2` extending or replacing V1:
  - Adds `OriginalImageWidth`, `OriginalImageHeight` (int)
  - `Version = "V2"`
  - Keeps all existing V1 fields
- `FromJson()` version router handles both V1 and V2
- V1 metadata remains readable (backward compat)

### Modified: `Model/BitmapExtensions.cs`
- `AddMetadata()` now writes V2 JSON
- New method: `RestoreFromPatches(this Bitmap bitmap, List<PatchData> patches) → Bitmap`
  — crops to original dimensions, pastes patches back

### Modified: `ViewModels/FileManager.cs`
- `ExecuteSaveImage()`:
  - Gets bitmap + patches from `ToNumberedBitmap`
  - Encodes bitmap to JPEG `byte[]` in memory (not directly to file)
  - Calls `AppSegmentIO.WriteAutoNumSegments(jpegBytes, patches)`
  - Writes final bytes to file
- `ExecuteOpenImage()`:
  - After loading bitmap and detecting V2 metadata:
    - Reads patches from APP4 segments
    - Restores clean base image via `RestoreFromPatches`
    - No longer needs original file for V2 images
  - V1 images: falls back to existing original-file-based flow

### Modified: `ViewModels/ImageVM.cs`
- No structural changes needed; `InitFromMetadata` works the same since it receives
  the metadata + a clean bitmap (now reconstructed instead of loaded from original file)

## JPEG APP4 Segment Binary Layout

```
Segment header (inserted by us into JPEG byte stream):
  FF E4              — APP4 marker
  XX XX              — Segment length (big-endian uint16, includes length bytes but not marker)

Segment payload:
  41 75 74 6F 4E 75 6D 00   — Magic: "AutoNum\0" (8 bytes)
  SS SS                       — Segment index (uint16 LE, 0-based)
  TT TT                       — Total segments (uint16 LE)
  
  [If segment 0:]
  PP PP                       — Patch count (uint16 LE)
  
  [Patch data, possibly spanning multiple segments:]
  For each patch:
    XX XX XX XX               — X position (float32 LE)
    YY YY YY YY               — Y position (float32 LE)
    WW WW WW WW               — Width (float32 LE)
    HH HH HH HH               — Height (float32 LE)
    LL LL LL LL               — PNG data length (uint32 LE)
    [PNG bytes...]             — Raw PNG image data
```

## Key Design Decisions
1. **Patches are captured from `bmpFinal` before label drawing** (Approach B) — avoids
   JPEG round-trip complexity. If visible seams appear, switch to Approach A (encode to
   JPEG in memory first, decode, then capture patches).
2. **PNG encoding for patches** — lossless, good compression for small regions, trivial
   to encode/decode with GDI+.
3. **APP4 segment** — unused by any known standard, collision-free with `AutoNum\0` prefix.
4. **V1 backward compatibility** — V1 metadata continues to work via original-file fallback.
5. **Title/footer don't need patches** — just store original image dimensions and crop.

## Risks & Mitigations
- **EXIF UserComment size**: V2 JSON is similar size to V1 (no patch data in JSON). No risk.
- **APP4 size**: 18 labels × ~60×60px PNG ≈ 30-80 KB. Fits in 1-2 APP4 segments easily.
- **JPEG artifacts at patch boundaries**: Expected to be imperceptible at quality 85+.
  Fallback: Approach A (in-memory JPEG round-trip before capture).
- **Chained segments**: Needed only for edge cases (huge label diameters, many labels).
  Implement from the start to avoid retrofitting.
