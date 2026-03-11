using AutoNumber.ViewModels;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace AutoNumber.Model
{
    /// <summary>
    /// Result of <see cref="ExtensionMethods.ToNumberedBitmap"/> containing both the
    /// rendered composite bitmap and the pixel patches captured before labels were drawn.
    /// </summary>
    internal record NumberedBitmapResult(Bitmap Bitmap, List<PatchData> Patches) : IDisposable
    {
        public void Dispose() => Bitmap.Dispose();
    }

    public static class ExtensionMethods
    {      
        private static float toGdiFontSize(this double sz, Graphics gFinal)
        {
            return 0.711f * (float)sz * 96 / gFinal.DpiX;
        }

        public static void drawLabels(List<MarkerLabel> labels, Bitmap bmp, int offset)
        {
            using var gFinal = Graphics.FromImage(bmp);

            float fontSize = MarkerLabel.Style.FontSize.toGdiFontSize(gFinal);            
            using Brush fillBrush = new SolidBrush(MarkerLabel.Style.BackgroundColor);
            using Brush textBrush = new SolidBrush(MarkerLabel.Style.FontColor);
            using Font font = new Font("Calibri", fontSize);

            StringFormat format = new StringFormat(StringFormat.GenericDefault);

            foreach (var label in labels)
            {
                PointF circlePos = new PointF((float)label.X, (float)label.Y + offset);
                SizeF circleSize = new SizeF(MarkerLabel.Style.Diameter, MarkerLabel.Style.Diameter);
                RectangleF BB = new RectangleF(circlePos, circleSize);
                gFinal.FillEllipse(fillBrush, BB);                

                SizeF textSize = gFinal.MeasureString(label.Number.ToString(), font, circlePos, format);
                PointF textPos = new(circlePos.X + (BB.Width - textSize.Width) / 2.0f, circlePos.Y + (BB.Height - textSize.Height) / 2.0f);
                gFinal.DrawString(label.Number.ToString(), font, textBrush, textPos, format);
            }
        }
        public static void drawNames(List<TextLabel> names, Bitmap bmp, Font font, int offset)
        {
            using var g = Graphics.FromImage(bmp);

            using Brush textBrush = new SolidBrush(TextLabel.Style.FontColor);
            StringFormat format = new StringFormat(StringFormat.GenericDefault);

            foreach (var name in names)
            {
                PointF pos = new PointF((float)name.X, offset + (float)name.Y);
                g.DrawString(name.Person.FullName, font, textBrush, pos, format);
            }
        }

        /// <summary>
        /// Captures a rectangular pixel region from the bitmap and encodes it as PNG.
        /// Adds a 1px margin to cover GDI+ anti-aliasing bleed at ellipse edges.
        /// </summary>
        private static PatchData capturePatch(Bitmap bmp, RectangleF region)
        {
            // Expand to integer pixel grid: floor(origin) → ceil(origin + size), plus 1px AA margin
            const int margin = 1;
            int left = Math.Max(0, (int)Math.Floor(region.X) - margin);
            int top = Math.Max(0, (int)Math.Floor(region.Y) - margin);
            int right = Math.Min(bmp.Width, (int)Math.Ceiling(region.X + region.Width) + margin);
            int bottom = Math.Min(bmp.Height, (int)Math.Ceiling(region.Y + region.Height) + margin);

            var rect = new Rectangle(left, top, right - left, bottom - top);
            using var patch = bmp.Clone(rect, bmp.PixelFormat);
            using var ms = new MemoryStream();
            patch.Save(ms, ImageFormat.Png);
            return new PatchData(rect.X, rect.Y, rect.Width, rect.Height, ms.ToArray());
        }

        internal static NumberedBitmapResult? ToNumberedBitmap(this ImageVM model, LabelManager lm, NameManager nm, TitleManager tm)
        {
            if (model?.Bitmap is null) return null;

            var names = model.Persons.Select(p => p.Name).ToList();
            var labels = model.Persons.Select(p => p.Label).ToList();

            bool hasNames = nm.IsEnabled && names.Count > 0;
            bool hasTitle = tm.IsEnabled && !string.IsNullOrEmpty(tm.Title);

            var oldWidth = model.Bitmap.Width;
            var oldHeight = model.Bitmap.Height;
            int titleHeight = hasTitle ? (int)model.TitleRegionHeight : 0;
            int footerHeight = hasNames ? (int)model.NamesRegionHeight : 0;

            int newHeight = oldHeight + titleHeight + footerHeight;
            var bmpFinal = new Bitmap(oldWidth, newHeight);
            using var g = Graphics.FromImage(bmpFinal);

            g.DrawImage(model.Bitmap, new Rectangle(0, titleHeight, oldWidth, oldHeight), new Rectangle(0, 0, oldWidth, oldHeight), GraphicsUnit.Pixel);

            if (hasTitle)
            {
                using Brush bg = new SolidBrush(tm.BackgroundColor);
                using Brush fg = new SolidBrush(tm.TitleFontColor);
                RectangleF BB = new RectangleF(0, 0, bmpFinal.Width, titleHeight);

                g.FillRectangle(bg, BB);

                var fontSize = tm.TitleFontSize.toGdiFontSize(g);
                using var font = new Font(tm.TitleFontFamily, fontSize);

                StringFormat format = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center
                };

                g.DrawString(tm.Title, font, fg, BB, format);
            }
            if (hasNames)
            {
                using Brush bg = new SolidBrush(nm.BackgroundColor);
                using Brush fg = new SolidBrush(nm.FontColor);
                g.FillRectangle(bg, new Rectangle(0, newHeight - footerHeight, bmpFinal.Width, footerHeight));

                var fontSize = TextLabel.Style.FontSize.toGdiFontSize(g);
                using var font = new Font(nm.FontFamily, fontSize);
                drawNames(names, bmpFinal, font, titleHeight);
            }

            // Capture patches BEFORE drawing labels (Approach B — pre-JPEG pixels)
            g.Flush(); // ensure all drawing above is committed
            var patches = new List<PatchData>(labels.Count);
            foreach (var label in labels)
            {
                var region = new RectangleF(
                    (float)label.X,
                    (float)label.Y + titleHeight,
                    MarkerLabel.Style.Diameter,
                    MarkerLabel.Style.Diameter);
                patches.Add(capturePatch(bmpFinal, region));
            }

            drawLabels(labels, bmpFinal, titleHeight);

            bmpFinal.CopyMetadataFrom(model.OriginalPropertyItems);
            bmpFinal.AddMetadata(model, lm, nm, tm);

            return new NumberedBitmapResult(bmpFinal, patches);
        }
    }
}
