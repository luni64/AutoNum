namespace AutoNumber.Model;

internal sealed record NameTableLayoutEntry(string NameText, double DesiredContentHeight);

internal static class NameTableLayoutEngine
{
    public static NameTableLayoutResult BuildLayout(IReadOnlyList<NameTableLayoutEntry> entries, NameTableLayoutOptions options)
    {
        if (entries.Count == 0)
        {
            return new NameTableLayoutResult([], 0, 0, Math.Max(1, options.ColumnCount));
        }

        var columnCount = Math.Max(1, options.ColumnCount);
        var totalWidth = Math.Max(1, options.TotalWidth);
        var columnWidth = totalWidth / columnCount;

        var rowCount = (int)Math.Ceiling(entries.Count / (double)columnCount);
        var rowHeights = new double[rowCount];

        for (var index = 0; index < entries.Count; index++)
        {
            var rowIndex = index / columnCount;
            var desiredContentHeight = Math.Max(0, entries[index].DesiredContentHeight);
            var desiredCellHeight = Math.Max(options.MinRowHeight, desiredContentHeight + 2 * options.CellPaddingY);
            rowHeights[rowIndex] = Math.Max(rowHeights[rowIndex], desiredCellHeight);
        }

        var rowTopOffsets = new double[rowCount];
        var runningTop = options.StartY;
        for (var row = 0; row < rowCount; row++)
        {
            rowTopOffsets[row] = runningTop;
            runningTop += rowHeights[row];
        }

        var cells = new List<NameTableCellLayout>(entries.Count);
        for (var index = 0; index < entries.Count; index++)
        {
            var rowIndex = index / columnCount;
            var columnIndex = index % columnCount;
            var x = columnIndex * columnWidth;
            var y = rowTopOffsets[rowIndex];
            var height = rowHeights[rowIndex];
            var numberColumnWidth = Math.Min(columnWidth, NamesTableLayout.ResolveNumberColumnWidth(columnWidth));
            var nameColumnWidth = Math.Max(1, columnWidth - numberColumnWidth);

            cells.Add(new NameTableCellLayout(
                X: x,
                Y: y,
                Width: columnWidth,
                Height: height,
                NumberColumnWidth: numberColumnWidth,
                NameColumnWidth: nameColumnWidth));
        }

        var totalHeight = runningTop - options.StartY;
        return new NameTableLayoutResult(cells, totalHeight, rowCount, columnCount);
    }
}
