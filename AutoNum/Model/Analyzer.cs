using AutoNumber.ViewModels;
using System.ComponentModel;
using System.Drawing;


namespace AutoNumber.Model
{
    public static class Analyzer
    {
        private static SizeF measureString(string text, Font font, GraphicsUnit unit = GraphicsUnit.Display)
        {
            using var bmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bmp);
            g.PageUnit = unit;
            return g.MeasureString(text, font);
        }

        public static float GetCircumscribingDiameter(string s, Font font)
        {
            SizeF size = measureString(s, font);
            return (float)Math.Sqrt(size.Width * size.Width + size.Height * size.Height);
        }

        public static (float d_max, T? item) GetLargestItem<T>(IEnumerable<T> list, Func<T, string?> selector, Font font)
        {
            float d_max = 0;
            T? largestItem = list.FirstOrDefault();
            foreach (T item in list)
            {
                string? str = selector(item);
                if (str is null) continue;

                var r = GetCircumscribingDiameter(str, font);
                if (r > d_max)
                {
                    d_max = r;
                    largestItem = item;
                }
            }
            return (d_max, largestItem);
        }

        public static SizeF GetLargestBoundingBox<T>(IEnumerable<T> items, Func<T, string?> selector, Font font)
        {
            SizeF largest = new SizeF();
            foreach (T item in items)
            {
                string? str = selector(item);
                if (str is null) continue;

                SizeF thisSize = measureString(str, font, GraphicsUnit.Point);
                largest.Width = Math.Max(largest.Width, thisSize.Width);
                largest.Height = Math.Max(largest.Height, thisSize.Height);
            }
            return largest;
        }

        public static double GetTextBlockHeight(string text, FontFamily fontFamily, double fontSize)
        {
            if (string.IsNullOrWhiteSpace(text) || !double.IsFinite(fontSize) || fontSize <= 0)
            {
                return 0;
            }

            using var font = new Font(fontFamily, (float)fontSize);
            SizeF thisSize = measureString(text, font, GraphicsUnit.Point);
            return thisSize.Height;
        }

        public static double PlaceTitle(TitleManager tm, ImageInfoManager iim)
        {
            return GetTextBlockHeight(tm.Title, tm.TitleFontFamily, tm.TitleFontSize)
                + GetTextBlockHeight(iim.ImageInfo, iim.ImageInfoFontFamily, iim.ImageInfoFontSize);
        }

        public static NamePlacementResult PlacePersonNames(
            ICollectionView persons,
            double width,
            double startY,
            int columnCount,
            bool includeRowDividers,
            Func<int, string> rowDividerTextFactory)
        {
            var items = persons.OfType<Person>().ToList();
            if (items.Count == 0)
            {
                return new NamePlacementResult(0, []);
            }

            var orderedItems = items
                .OrderBy(person => person.Row <= 0 ? int.MaxValue : person.Row)
                .ThenBy(person => person.Label.Number)
                .ToList();

            var fontSize = double.IsFinite(TextLabel.Style.FontSize) && TextLabel.Style.FontSize > 0
                ? TextLabel.Style.FontSize
                : 1.0;

            using Font font = new Font(TextLabel.Style.FontFamily, (float)fontSize, FontStyle.Regular, GraphicsUnit.Pixel);

            var options = NameTableLayoutOptions.Default(width, startY, fontSize, Math.Clamp(columnCount, 1, 4));
            var columnWidth = Math.Max(1, options.TotalWidth / Math.Max(1, options.ColumnCount));
            var numberColumnWidth = Math.Min(columnWidth, NamesTableLayout.ResolveNumberColumnWidth(columnWidth));
            var contentWidth = Math.Max(1, columnWidth - numberColumnWidth - 2 * options.CellPaddingX);

            var entries = new List<NameTableLayoutEntry>();
            var entryPersons = new List<Person?>();
            var dividerRowsByEntry = new List<int>();

            var assignedRowGroups = orderedItems
                .Where(person => person.Row > 0)
                .GroupBy(person => person.Row)
                .OrderBy(group => group.Key)
                .ToList();

            foreach (var rowGroup in assignedRowGroups)
            {
                if (includeRowDividers)
                {
                    var dividerText = rowDividerTextFactory(rowGroup.Key);
                    var dividerHeight = MeasureWrappedTextHeight(dividerText, font, (float)Math.Max(1, options.TotalWidth - 2 * options.CellPaddingX));

                    entries.Add(new NameTableLayoutEntry(dividerText, dividerHeight));
                    entryPersons.Add(null);
                    dividerRowsByEntry.Add(rowGroup.Key);

                    for (var filler = 1; filler < options.ColumnCount; filler++)
                    {
                        entries.Add(new NameTableLayoutEntry(string.Empty, 0));
                        entryPersons.Add(null);
                        dividerRowsByEntry.Add(0);
                    }
                }

                foreach (var person in rowGroup.OrderBy(person => person.Label.Number))
                {
                    var nameText = person.Name.Text ?? string.Empty;
                    var nameHeight = MeasureWrappedTextHeight(nameText, font, (float)contentWidth);
                    entries.Add(new NameTableLayoutEntry(nameText, nameHeight));
                    entryPersons.Add(person);
                    dividerRowsByEntry.Add(0);
                }

                var remainder = entries.Count % options.ColumnCount;
                if (remainder > 0)
                {
                    for (var filler = remainder; filler < options.ColumnCount; filler++)
                    {
                        entries.Add(new NameTableLayoutEntry(string.Empty, 0));
                        entryPersons.Add(null);
                        dividerRowsByEntry.Add(0);
                    }
                }
            }

            foreach (var person in orderedItems.Where(person => person.Row <= 0))
            {
                var nameText = person.Name.Text ?? string.Empty;
                var nameHeight = MeasureWrappedTextHeight(nameText, font, (float)contentWidth);
                entries.Add(new NameTableLayoutEntry(nameText, nameHeight));
                entryPersons.Add(person);
                dividerRowsByEntry.Add(0);
            }

            var layout = NameTableLayoutEngine.BuildLayout(entries, options);
            var dividerItems = new List<NameListDividerRenderItem>();

            for (var index = 0; index < layout.Cells.Count; index++)
            {
                var cell = layout.Cells[index];
                var person = entryPersons[index];

                if (person is not null)
                {
                    person.Name.X = cell.X;
                    person.Name.Y = cell.Y;
                    person.Name.W = cell.Width;
                    person.Name.H = cell.Height;
                    person.Name.Visible = true;
                    continue;
                }

                var row = dividerRowsByEntry[index];
                if (row > 0)
                {
                    dividerItems.Add(new NameListDividerRenderItem(
                        Row: row,
                        Text: rowDividerTextFactory(row),
                        X: 0,
                        Y: cell.Y,
                        Width: options.TotalWidth,
                        Height: cell.Height));
                }
            }

            return new NamePlacementResult(layout.TotalHeight, dividerItems);
        }

        private static double MeasureWrappedTextHeight(string text, Font font, float maxWidth)
        {
            using var bmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bmp);
            g.PageUnit = GraphicsUnit.Pixel;

            var value = string.IsNullOrWhiteSpace(text) ? "______________" : text;
            var measured = g.MeasureString(value, font, new SizeF(maxWidth, float.MaxValue));
            return Math.Max(1, measured.Height);
        }
    }
}
