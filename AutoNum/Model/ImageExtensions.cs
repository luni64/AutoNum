using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;

public static class ImageExtensions
{
    public static Mat AddNamesMultiColumnOptimized(
        this Mat original,
        List<string> names)
    {
        // Convert the original Mat to Bitmap to use GDI+ for measuring & drawing text (umlauts, etc.)
        Bitmap bmpOriginal = original.ToBitmap();
        int imgWidth = bmpOriginal.Width;
        int imgHeight = bmpOriginal.Height;

        // We'll track the best arrangement:
        float bestFontSize = 0f;
        int bestColumns = 1;

        // For performance, define your maximum number of columns. 
        // Going up to names.Count is possible, but can be slow if there are many names.
        // Here, we pick the min of names.Count or 10 as an upper bound.
        int maxPossibleColumns = Math.Min(names.Count, 10);

        // We'll do a simple incremental search for font sizes.
        // For production, you may want a binary search approach for better performance.
        for (int colCount = 1; colCount <= maxPossibleColumns; colCount++)
        {
            // We'll step from e.g. 4pt up to 200pt in increments of 2pt
            float localBestForThisColCount = 0f;

            for (float testSize = 4f; testSize <= 200f; testSize += 2f)
            {
                if (CheckIfFits(names, colCount, testSize, imgWidth, imgHeight))
                {
                    // If it fits, update local best
                    localBestForThisColCount = Math.Max(localBestForThisColCount, testSize);
                }
                else
                {
                    // As soon as we exceed constraints, no need to test larger fonts for this colCount
                    break;
                }
            }

            // Compare with the global best
            if (localBestForThisColCount > bestFontSize)
            {
                bestFontSize = localBestForThisColCount;
                bestColumns = colCount;
            }
        }

        // If we never found anything above 4pt, pick some fallback
        if (bestFontSize < 4f)
        {
            bestFontSize = 12f;
            bestColumns = 1;
        }

        // Now we do one final rendering pass with the best columns & best font size
        return RenderMultiColumnTable(names, bestColumns, bestFontSize, bmpOriginal);
    }

    /// <summary>
    /// Checks if a given font size & number of columns fits the constraints:
    /// 1) Table height < 1/3 of original
    /// 2) Table width < original image width
    /// </summary>
    private static bool CheckIfFits(
        List<string> names,
        int columns,
        float fontSize,
        int imgWidth,
        int imgHeight)
    {
        // Basic GDI+ measure: we create a small placeholder to get a Graphics context
        using (var measureBmp = new Bitmap(1, 1))
        using (var g = Graphics.FromImage(measureBmp))
        using (var testFont = new Font("Arial", fontSize, FontStyle.Regular, GraphicsUnit.Point))
        {
            g.PageUnit = GraphicsUnit.Pixel; // measure in pixel units

            // 1) Split the names into columns
            // We'll create a List<List<string>> columnsData
            List<List<string>> columnsData = DistributeIntoColumns(names, columns);

            // 2) Determine line height and spacing
            float lineSpacing = testFont.Height * 0.2f; // e.g., 20% of the font height
            float lineHeight = testFont.Height + lineSpacing;

            // 3) For each column, measure the widest item
            float[] colWidths = new float[columns];
            int maxRows = 0;

            for (int c = 0; c < columns; c++)
            {
                List<string> colNames = columnsData[c];
                if (colNames.Count > maxRows) maxRows = colNames.Count;

                float maxWidth = 0f;
                foreach (var name in colNames)
                {
                    var sizeF = g.MeasureString(name, testFont);
                    if (sizeF.Width > maxWidth) maxWidth = sizeF.Width;
                }
                colWidths[c] = maxWidth;
            }

            // 4) Compute total needed height = maxRows * lineHeight + top/bottom margin
            float topMargin = 20f;
            float bottomMargin = 20f;
            float totalNeededHeight = maxRows * lineHeight + topMargin + bottomMargin;

            if (totalNeededHeight > (imgHeight / 3f))
                return false; // fails height constraint

            // 5) Compute total needed width
            float leftMargin = 20f;
            float rightMargin = 20f;
            float spacingBetweenCols = 40f;

            // Sum colWidths plus spacing
            // We'll have (columns - 1) spacers if columns > 1
            float totalContentWidth = 0f;
            for (int c = 0; c < columns; c++)
            {
                totalContentWidth += colWidths[c];
                if (c < columns - 1)
                    totalContentWidth += spacingBetweenCols;
            }
            float totalNeededWidth = leftMargin + totalContentWidth + rightMargin;
            if (totalNeededWidth > imgWidth)
                return false; // fails width constraint
        }

        // If we reach here, it fits
        return true;
    }

    /// <summary>
    /// Splits 'names' into the specified number of columns as evenly as possible.
    /// For c columns, each column has about names.Count/c items, with the last column possibly shorter or longer.
    /// </summary>
    private static List<List<string>> DistributeIntoColumns(List<string> names, int c)
    {
        // We'll do a straightforward approach:
        // number of items per column = ceil( names.Count / c )
        // then fill columns in a linear fashion
        List<List<string>> columns = new List<List<string>>(c);

        int total = names.Count;
        // max rows per column
        int rowsPerColumn = (int)Math.Ceiling((double)total / c);

        int index = 0;
        for (int col = 0; col < c; col++)
        {
            var colList = new List<string>(rowsPerColumn);
            for (int row = 0; row < rowsPerColumn; row++)
            {
                if (index < total)
                {
                    colList.Add(names[index]);
                    index++;
                }
                else break;
            }
            columns.Add(colList);
        }

        return columns;
    }

    /// <summary>
    /// Renders the table at the bottom of the image using the chosen # of columns and font size.
    /// Returns a new Mat with the expanded bottom area.
    /// </summary>
    private static Mat RenderMultiColumnTable(
        List<string> names,
        int columns,
        float fontSize,
        Bitmap bmpOriginal)
    {
        int imgWidth = bmpOriginal.Width;
        int imgHeight = bmpOriginal.Height;

        using (var measureBmp = new Bitmap(1, 1))
        using (var gMeasure = Graphics.FromImage(measureBmp))
        using (var finalFont = new Font("Arial", fontSize, FontStyle.Regular, GraphicsUnit.Point))
        {
            gMeasure.PageUnit = GraphicsUnit.Pixel;

            // Distribute the names
            var columnsData = DistributeIntoColumns(names, columns);

            // measure line & spacing
            float lineSpacing = finalFont.Height * 0.2f;
            float lineHeight = finalFont.Height + lineSpacing;

            // measure how many rows we need => largest column length
            int maxRows = 0;
            float[] colWidths = new float[columns];

            for (int c = 0; c < columns; c++)
            {
                var colNames = columnsData[c];
                if (colNames.Count > maxRows) maxRows = colNames.Count;

                float maxWidth = 0f;
                foreach (var name in colNames)
                {
                    var sizeF = gMeasure.MeasureString(name, finalFont);
                    if (sizeF.Width > maxWidth) maxWidth = sizeF.Width;
                }
                colWidths[c] = maxWidth;
            }

            float topMargin = 20f;
            float bottomMargin = 20f;
            float totalHeight = maxRows * lineHeight + topMargin + bottomMargin;
            int newHeight = (int)(imgHeight + totalHeight);

            // Create final output
            Bitmap bmpFinal = new Bitmap(imgWidth, newHeight, PixelFormat.Format24bppRgb);
            using (var gFinal = Graphics.FromImage(bmpFinal))
            {
                gFinal.Clear(Color.White);

                // Draw original at top
                gFinal.DrawImage(bmpOriginal, 0, 0);

                // place columns with leftMargin, spacing, rightMargin
                float leftMargin = 20f;
                float spacingBetweenCols = 40f;

                float currentX = leftMargin;
                float startY = imgHeight + topMargin;

                // Render each column
                for (int c = 0; c < columns; c++)
                {
                    var colNames = columnsData[c];

                    // Draw each row
                    for (int r = 0; r < colNames.Count; r++)
                    {
                        float y = startY + r * lineHeight;
                        gFinal.DrawString(colNames[r], finalFont, Brushes.Black, currentX, y);
                    }

                    // Move X for next column
                    currentX += colWidths[c] + spacingBetweenCols;
                }
            }

            // Convert final bitmap back to Mat if desired
            return bmpFinal.ToMat();
        }
    }
}
