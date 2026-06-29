using AutoNumber.ViewModels;
using System.Drawing;
using System.Drawing.Drawing2D;
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
            using Pen edgePen = new Pen(MarkerLabel.Style.EdgeColor, Math.Max(2f, MarkerLabel.Style.Diameter / 25f));

            foreach (var label in labels)
            {
                PointF circlePos = new PointF((float)label.X, (float)label.Y + offset);
                SizeF circleSize = new SizeF(MarkerLabel.Style.Diameter, MarkerLabel.Style.Diameter);
                RectangleF BB = new RectangleF(circlePos, circleSize);
                gFinal.FillEllipse(fillBrush, BB);
                gFinal.DrawEllipse(edgePen, BB.X, BB.Y, BB.Width, BB.Height);

                drawCenteredGlyph(gFinal, label.Number.ToString(), MarkerLabel.Style.FontFamily, fontSize, textBrush, BB);
            }
        }
        private static void drawCenteredGlyph(Graphics graphics, string text, FontFamily fontFamily, float fontSize, Brush brush, RectangleF bounds)
        {
            using var path = new GraphicsPath();
            path.AddString(text, fontFamily, (int)FontStyle.Regular, fontSize, PointF.Empty, StringFormat.GenericDefault);

            var glyphBounds = path.GetBounds();
            var dx = bounds.X + (bounds.Width / 2f) - (glyphBounds.X + glyphBounds.Width / 2f);
            var dy = bounds.Y + (bounds.Height / 2f) - (glyphBounds.Y + glyphBounds.Height / 2f);

            using var matrix = new Matrix();
            matrix.Translate(dx, dy);
            path.Transform(matrix);
            graphics.FillPath(brush, path);
        }

        private static void drawTopTextBlock(Graphics graphics, string text, FontFamily fontFamily, float fontSize, Color backgroundColor, Color foregroundColor, RectangleF bounds)
        {
            using Brush bg = new SolidBrush(backgroundColor);
            using Brush fg = new SolidBrush(foregroundColor);
            using var font = new Font(fontFamily, fontSize);
            StringFormat format = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Center
            };

            graphics.FillRectangle(bg, bounds);
            graphics.DrawString(text, font, fg, bounds, format);
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

        private static void drawImageId(Graphics g, Bitmap bmp, ImageIdManager idm, int titleOffset, int imageHeight)
        {
            if (string.IsNullOrWhiteSpace(idm.ImageId) || !idm.IsEnabled)
            {
                return;
            }

            var fontSize = idm.FontSize.toGdiFontSize(g);
            using var font = new Font(idm.FontFamily, fontSize);
            using Brush bg = new SolidBrush(idm.BackgroundColor);
            using Brush fg = new SolidBrush(idm.FontColor);

            var bounds = new RectangleF(0, titleOffset + imageHeight, bmp.Width, (float)idm.LineHeight);
            g.FillRectangle(bg, bounds);

            var textSize = g.MeasureString(idm.ImageId, font);
            const float textPadding = 8f;
            var textX = bounds.Right - textSize.Width - textPadding;
            var textY = bounds.Top + Math.Max(0, (bounds.Height - textSize.Height) / 2f);
            g.DrawString(idm.ImageId, font, fg, new PointF(textX, textY));
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

        internal static Bitmap? ToPhotoWithLabelsBitmap(this ImageVM model)
        {
            if (model?.Bitmap is null)
            {
                return null;
            }

            var labels = model.Persons.Select(p => p.Label).ToList();
            var bitmap = new Bitmap(model.Bitmap);
            drawLabels(labels, bitmap, 0);
            return bitmap;
        }

        internal static NumberedBitmapResult? ToNumberedBitmap(this ImageVM model, LabelManager lm, NameManager nm, TitleManager tm, ImageInfoManager iim, ImageIdManager idm)
        {
            if (model?.Bitmap is null) return null;

            var names = model.Persons.Select(p => p.Name).ToList();
            var labels = model.Persons.Select(p => p.Label).ToList();

            bool hasNames = nm.IsEnabled && names.Count > 0;
            bool hasImageId = idm.IsEnabled && !string.IsNullOrWhiteSpace(idm.ImageId);
            bool hasTitle = tm.IsEnabled && !string.IsNullOrEmpty(tm.Title);
            bool hasImageInfo = iim.IsEnabled && !string.IsNullOrEmpty(iim.ImageInfo);
            bool hasTopText = hasTitle || hasImageInfo;
            bool hasFooter = hasNames || hasImageId;

            var oldWidth = model.Bitmap.Width;
            var oldHeight = model.Bitmap.Height;
            int titleHeight = hasTopText ? (int)model.TitleRegionHeight : 0;
            int imageIdHeight = hasImageId ? (int)Math.Ceiling(idm.LineHeight) : 0;
            int namesHeight = hasNames ? (int)model.NamesRegionHeight : 0;
            int footerHeight = hasFooter ? imageIdHeight + namesHeight : 0;

            int newHeight = oldHeight + titleHeight + footerHeight;
            var bmpFinal = new Bitmap(oldWidth, newHeight);
            using var g = Graphics.FromImage(bmpFinal);

            g.DrawImage(model.Bitmap, new Rectangle(0, titleHeight, oldWidth, oldHeight), new Rectangle(0, 0, oldWidth, oldHeight), GraphicsUnit.Pixel);

            if (hasTopText)
            {
                var titleBlockHeight = hasTitle ? Analyzer.GetTextBlockHeight(tm.Title, tm.TitleFontFamily, tm.TitleFontSize) : 0;
                var infoBlockHeight = hasImageInfo ? Analyzer.GetTextBlockHeight(iim.ImageInfo, iim.ImageInfoFontFamily, iim.ImageInfoFontSize) : 0;
                var totalMeasuredHeight = Math.Max(1, titleBlockHeight + infoBlockHeight);
                float currentTop = 0;

                if (hasTitle)
                {
                    var blockHeight = hasImageInfo
                        ? (float)Math.Round(titleHeight * (titleBlockHeight / totalMeasuredHeight))
                        : titleHeight;
                    var titleBounds = new RectangleF(0, currentTop, bmpFinal.Width, blockHeight);
                    drawTopTextBlock(g, tm.Title, tm.TitleFontFamily, tm.TitleFontSize.toGdiFontSize(g), tm.BackgroundColor, tm.TitleFontColor, titleBounds);
                    currentTop += blockHeight;
                }

                if (hasImageInfo)
                {
                    var remainingHeight = Math.Max(0, titleHeight - currentTop);
                    var infoBounds = new RectangleF(0, currentTop, bmpFinal.Width, remainingHeight);
                    drawTopTextBlock(g, iim.ImageInfo, iim.ImageInfoFontFamily, iim.ImageInfoFontSize.toGdiFontSize(g), iim.BackgroundColor, iim.ImageInfoFontColor, infoBounds);
                }
            }
            if (hasFooter)
            {
                if (hasImageId)
                {
                    drawImageId(g, bmpFinal, idm, titleHeight, oldHeight);
                }

                if (hasNames)
                {
                    using Brush bg = new SolidBrush(nm.BackgroundColor);
                    g.FillRectangle(bg, new Rectangle(0, titleHeight + oldHeight + imageIdHeight, bmpFinal.Width, namesHeight));

                    var fontSize = TextLabel.Style.FontSize.toGdiFontSize(g);
                    using var font = new Font(nm.FontFamily, fontSize);
                    drawNames(names, bmpFinal, font, titleHeight);
                }
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
            bmpFinal.AddMetadata(model, lm, nm, tm, iim, idm);

            return new NumberedBitmapResult(bmpFinal, patches);
        }
    }
}
