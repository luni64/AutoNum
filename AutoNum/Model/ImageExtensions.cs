using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;

public static class ImageExtensions
{
    public static Mat AddNamesAtBottomInTwoColumnsGdi(this Mat original, List<string> names)
    {
        // 1) The “table” (text area) must be < 1/3 of original image height.
        // 2) Two columns fit the original image width.
        // 3) We want the text as large as possible, under those constraints.

        // Convert original Mat to Bitmap for GDI+ drawing/measurement.
        Bitmap bmpOriginal = original.ToBitmap();

        int imgWidth = bmpOriginal.Width;
        int imgHeight = bmpOriginal.Height;

        // We'll store the best found font size in points
        float bestFontSize = 0f;

        // Two columns: if odd count, first column will have one extra entry.
        int lines = (names.Count + 1) / 2;

        // Because we’re using GDI+ `Graphics.MeasureString`, we can test from, say,
        // 4pt up to 200pt in small increments. Adjust as desired for performance/accuracy.
        for (float testSize = 4f; testSize <= 200f; testSize += 2f)
        {
            using (var testFont = new Font("Courier New", testSize, FontStyle.Regular, GraphicsUnit.Point))
            {
                // We'll measure line-by-line in GDI+.
                // For line spacing, we can add a small multiple of testFont.Height.
                float lineSpacing = testFont.Height * 0.2f; // e.g., 20% of line height
                float lineHeight = testFont.Height + lineSpacing;

                // total vertical space needed = lines * lineHeight + some margin
                float totalTextHeight = lines * lineHeight + 20f;

                // 1) Check height constraint
                if (totalTextHeight > imgHeight / 3f)
                {
                    // too tall, so break
                    break;
                }

                // Now measure the width of each name to find the max for columns.
                // We'll do it separately for col1 (first 'lines') and col2 (the rest).
                var col1 = names.GetRange(0, lines);
                var col2 = names.GetRange(lines, names.Count - lines);

                // We must do a dummy Graphics object to measure string widths.
                // The typical approach is to create a temporary “compatible” in-memory bitmap.
                using (var measureBmp = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(measureBmp))
                {
                    g.PageUnit = GraphicsUnit.Pixel; // measure in pixels

                    float maxWidthCol1 = 0f;
                    foreach (var name in col1)
                    {
                        // measure string in GDI+
                        var sizeF = g.MeasureString(name, testFont);
                        if (sizeF.Width > maxWidthCol1) maxWidthCol1 = sizeF.Width;
                    }

                    float maxWidthCol2 = 0f;
                    foreach (var name in col2)
                    {
                        var sizeF = g.MeasureString(name, testFont);
                        if (sizeF.Width > maxWidthCol2) maxWidthCol2 = sizeF.Width;
                    }

                    // compute total needed width for both columns with margin
                    float leftMargin = 20f;
                    float spacingBetweenCols = 40f;
                    float rightMargin = 20f;

                    float col2X = leftMargin + maxWidthCol1 + spacingBetweenCols;
                    float totalNeededWidth = col2X + maxWidthCol2 + rightMargin;

                    // 2) Check width constraint
                    if (totalNeededWidth > imgWidth)
                    {
                        // too wide => break
                        break;
                    }
                }
            }

            // If we reach this point, the testSize is valid => record it
            bestFontSize = Math.Max(bestFontSize, testSize);
        }

        // If we never found a valid size above 4pt, fallback
        if (bestFontSize < 4f) bestFontSize = 12f;

        // Now do one final pass using bestFontSize
        using (var finalFont = new Font("Arial", bestFontSize, FontStyle.Regular, GraphicsUnit.Point))
        {
            // measure line spacing
            float lineSpacing = finalFont.Height * 0.2f;
            float lineHeight = finalFont.Height + lineSpacing;

            // We'll add enough space at the bottom for all lines + margin
            float extraHeight = lines * lineHeight + 20f;

            // The new total image height
            int newHeight = (int)(imgHeight + extraHeight);

            // Create final new bitmap
            Bitmap bmpFinal = new Bitmap(imgWidth, newHeight, PixelFormat.Format24bppRgb);

            // Fill with white
            using (var gFinal = Graphics.FromImage(bmpFinal))
            {
                gFinal.Clear(Color.White);

                // Draw the original image at (0,0).
                gFinal.DrawImage(bmpOriginal, new Rectangle(0, 0, imgWidth, imgHeight));

                // Recompute columns
                var col1 = names.GetRange(0, lines);
                var col2 = names.GetRange(lines, names.Count - lines);

                // Find max col widths with finalFont
                float maxWidthCol1 = 0f, maxWidthCol2 = 0f;
                using (var measureBmp = new Bitmap(1, 1))
                using (var gMeasure = Graphics.FromImage(measureBmp))
                {
                    foreach (var name in col1)
                    {
                        var sizeF = gMeasure.MeasureString(name, finalFont);
                        if (sizeF.Width > maxWidthCol1) maxWidthCol1 = sizeF.Width;
                    }
                    foreach (var name in col2)
                    {
                        var sizeF = gMeasure.MeasureString(name, finalFont);
                        if (sizeF.Width > maxWidthCol2) maxWidthCol2 = sizeF.Width;
                    }
                }

                float leftMargin = 20f;
                float spacingBetweenCols = 40f;
                float col1X = leftMargin;
                float col2X = col1X + maxWidthCol1 + spacingBetweenCols;
                float topMargin = 20f;

                // We'll start at bottom of original + topMargin
                float startY = imgHeight + topMargin;

                // Draw column text
                for (int i = 0; i < names.Count; i++)
                {
                    int colIndex = i / lines; // 0 => first col, 1 => second
                    int rowIndex = i % lines;
                    float x = (colIndex == 0) ? col1X : col2X;
                    float y = startY + rowIndex * lineHeight;

                    gFinal.DrawString(names[i], finalFont, Brushes.Black, x, y);
                }
            }

            // Convert final bitmap back to Mat if desired
            Mat resultMat = bmpFinal.ToMat();
            return resultMat;
        }
    }
}
