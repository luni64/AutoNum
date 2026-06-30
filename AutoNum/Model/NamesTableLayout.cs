namespace AutoNumber.Model;

internal static class NamesTableLayout
{
    public const float PdfBorderWidth = 0.5f;
    public const float CellPadding = 4f;

    public const float BitmapBorderWidth = 1f;
    public const float BitmapCellPaddingX = 8f;
    public const float BitmapCellPaddingY = 4f;
    public const float BitmapMinRowHeight = 20f;

    public const float NumberColumnRatio = 0.22f;
    public const float MinNumberColumnWidth = 60f;
    public const float MaxNumberColumnWidth = 140f;

    public static float ResolveNumberColumnWidth(double totalWidth)
    {
        var scaled = totalWidth * NumberColumnRatio;
        return (float)Math.Clamp(scaled, MinNumberColumnWidth, MaxNumberColumnWidth);
    }
}

internal sealed record NameTableLayoutOptions(
    int ColumnCount,
    double TotalWidth,
    double StartY,
    double FontSize,
    float MinRowHeight,
    float CellPaddingX,
    float CellPaddingY)
{
    public static NameTableLayoutOptions Default(double totalWidth, double startY, double fontSize, int columnCount = 1)
        => new(
            ColumnCount: Math.Clamp(columnCount, 1, 4),
            TotalWidth: Math.Max(1, totalWidth),
            StartY: startY,
            FontSize: fontSize,
            MinRowHeight: NamesTableLayout.BitmapMinRowHeight,
            CellPaddingX: NamesTableLayout.BitmapCellPaddingX,
            CellPaddingY: NamesTableLayout.BitmapCellPaddingY);
}

internal sealed record NameTableCellLayout(
    double X,
    double Y,
    double Width,
    double Height,
    double NumberColumnWidth,
    double NameColumnWidth);

internal sealed record NameTableLayoutResult(
    IReadOnlyList<NameTableCellLayout> Cells,
    double TotalHeight,
    int RowCount,
    int ColumnCount);
