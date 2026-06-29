namespace AutoNumber.Model;

internal static class NamesTableLayout
{
    public const float PdfNumberColumnWidth = 80f;
    public const float PdfBorderWidth = 0.5f;
    public const float CellPadding = 4f;

    public const float BitmapBorderWidth = 1f;
    public const float BitmapCellPaddingX = 8f;
    public const float BitmapCellPaddingY = 4f;
    public const float BitmapMinRowHeight = 20f;

    public static float ResolveBitmapNumberColumnWidth(double totalWidth)
    {
        var scaled = totalWidth * 0.22;
        return (float)Math.Clamp(scaled, 60, 140);
    }
}
