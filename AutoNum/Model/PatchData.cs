namespace AutoNumber.Model;

/// <summary>
/// Holds a rectangular pixel patch captured from the composite bitmap
/// before labels are drawn. Used to restore the clean base image on re-open.
/// </summary>
/// <param name="X">Left edge of the patch in the composite bitmap.</param>
/// <param name="Y">Top edge of the patch in the composite bitmap.</param>
/// <param name="Width">Patch width in pixels.</param>
/// <param name="Height">Patch height in pixels.</param>
/// <param name="PngBytes">Lossless PNG-encoded pixel data for the patch region.</param>
internal record PatchData(float X, float Y, float Width, float Height, byte[] PngBytes);
