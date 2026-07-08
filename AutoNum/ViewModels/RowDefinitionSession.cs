using System.Collections.ObjectModel;
using System.Drawing;

namespace AutoNumber.ViewModels
{
    public class RowDefinitionSession : BaseViewModel
    {
        public ObservableCollection<RowBoundary> Boundaries { get; } = [];

        public int RowCount
        {
            get => _rowCount;
            private set => SetProperty(ref _rowCount, value);
        }

        public double ImageWidth
        {
            get => _imageWidth;
            private set => SetProperty(ref _imageWidth, value);
        }

        public double ImageHeight
        {
            get => _imageHeight;
            private set => SetProperty(ref _imageHeight, value);
        }

        public void Initialize(int rowCount, double imageWidth, double imageHeight)
        {
            RowCount = Math.Max(1, rowCount);
            ImageWidth = Math.Max(0, imageWidth);
            ImageHeight = Math.Max(0, imageHeight);

            Boundaries.Clear();
            if (RowCount <= 1 || ImageHeight <= 0)
            {
                return;
            }

            for (var index = 1; index < RowCount; index++)
            {
                var y = ImageHeight * index / RowCount;
                Boundaries.Add(new RowBoundary(y, y));
            }
        }

        public void Restore(int rowCount, double imageWidth, double imageHeight, IEnumerable<RowBoundary> boundaries)
        {
            RowCount = Math.Max(1, rowCount);
            ImageWidth = Math.Max(0, imageWidth);
            ImageHeight = Math.Max(0, imageHeight);

            Boundaries.Clear();
            foreach (var boundary in boundaries)
            {
                Boundaries.Add(new RowBoundary(boundary.LeftY, boundary.RightY));
            }
        }

        public bool TryInsertBoundaryAtRightY(double rightY)
        {
            if (ImageHeight <= 0)
            {
                return false;
            }

            var clampedRightY = Math.Clamp(rightY, 0, ImageHeight);
            var insertIndex = 0;
            while (insertIndex < Boundaries.Count && Boundaries[insertIndex].RightY < clampedRightY)
            {
                insertIndex++;
            }

            var upperLeft = insertIndex == 0 ? 0.0 : Boundaries[insertIndex - 1].LeftY;
            var upperRight = insertIndex == 0 ? 0.0 : Boundaries[insertIndex - 1].RightY;
            var lowerLeft = insertIndex >= Boundaries.Count ? ImageHeight : Boundaries[insertIndex].LeftY;
            var lowerRight = insertIndex >= Boundaries.Count ? ImageHeight : Boundaries[insertIndex].RightY;

            const double minimumGap = 8.0;
            if (lowerRight - upperRight < minimumGap)
            {
                return false;
            }

            var rightSpan = lowerRight - upperRight;
            var t = Math.Abs(rightSpan) < 0.001
                ? 0.5
                : Math.Clamp((clampedRightY - upperRight) / rightSpan, 0.0, 1.0);

            var newLeft = upperLeft + (lowerLeft - upperLeft) * t;
            var newRight = upperRight + (lowerRight - upperRight) * t;

            Boundaries.Insert(insertIndex, new RowBoundary(newLeft, newRight));
            RowCount = Boundaries.Count + 1;
            ClampAllBoundaries();
            return true;
        }

        public bool TryDeleteRow(int row)
        {
            if (RowCount <= 1 || Boundaries.Count == 0 || row < 1 || row > RowCount)
            {
                return false;
            }

            var boundaryToRemove = Math.Min(Math.Max(0, row - 1), Boundaries.Count - 1);
            Boundaries.RemoveAt(boundaryToRemove);
            RowCount = Boundaries.Count + 1;
            ClampAllBoundaries();
            return true;
        }

        public IReadOnlyList<RowStripRow> GetRowsAtRightEdge()
        {
            var result = new List<RowStripRow>(Math.Max(1, RowCount));
            var top = 0.0;

            for (var index = 0; index < Boundaries.Count; index++)
            {
                var bottom = Math.Clamp(Boundaries[index].RightY, 0, ImageHeight);
                result.Add(new RowStripRow(index + 1, top, bottom));
                top = bottom;
            }

            result.Add(new RowStripRow(result.Count + 1, top, ImageHeight));
            return result;
        }

        public sealed record RowStripRow(int Row, double Top, double Bottom);

        public int ResolveRow(double x, double y)
        {
            var row = 1;
            foreach (var boundary in Boundaries)
            {
                if (y > boundary.GetYAtX(x, ImageWidth))
                {
                    row++;
                }
            }

            return row;
        }

        public int ResolveRow(Person person)
        {
            var anchor = person.GetRowAnchorPoint();
            return ResolveRow(anchor.X, anchor.Y);
        }

        public void ApplyToPersons(IEnumerable<Person> persons)
        {
            foreach (var person in persons)
            {
                person.Row = ResolveRow(person);
            }
        }

        public void ApplyPreviewToPersons(IEnumerable<Person> persons)
        {
            foreach (var person in persons)
            {
                var row = ResolveRow(person);
                person.RowPreviewActive = true;
                person.RowPreviewColor = GetPreviewColor(row);
            }
        }

        public void ClearPreview(IEnumerable<Person> persons)
        {
            foreach (var person in persons)
            {
                person.RowPreviewActive = false;
            }
        }

        public static Color GetPreviewColor(int row)
        {
            var palette = new[]
            {
                Color.FromArgb(255, 197, 227, 204),
                Color.FromArgb(255, 153, 208, 249),
                Color.FromArgb(255, 221, 219, 150),
                Color.FromArgb(255, 203, 213, 231),
                Color.FromArgb(255, 152, 230, 190),
                Color.FromArgb(255, 236, 203, 188),
                Color.FromArgb(255, 231, 189, 239),
                Color.FromArgb(255, 117, 227, 235)
            };

            return palette[Math.Max(0, row - 1) % palette.Length];
        }

        public void MoveBoundary(int index, double deltaY)
        {
            if (!TryGetBoundary(index, out var boundary))
            {
                return;
            }

            boundary.LeftY += deltaY;
            boundary.RightY += deltaY;
            ClampBoundary(index);
        }

        public void MoveBoundaryLeftAnchor(int index, double deltaY)
        {
            if (!TryGetBoundary(index, out var boundary))
            {
                return;
            }

            boundary.LeftY += deltaY;
            ClampBoundary(index);
        }

        public void MoveBoundaryRightAnchor(int index, double deltaY)
        {
            if (!TryGetBoundary(index, out var boundary))
            {
                return;
            }

            boundary.RightY += deltaY;
            ClampBoundary(index);
        }

        private void ClampBoundary(int index)
        {
            if (!TryGetBoundary(index, out var boundary))
            {
                return;
            }

            boundary.LeftY = ClampEdgeValue(index, boundary.LeftY, isLeftEdge: true);
            boundary.RightY = ClampEdgeValue(index, boundary.RightY, isLeftEdge: false);
        }

        private double ClampEdgeValue(int index, double value, bool isLeftEdge)
        {
            const double minimumGap = 8.0;

            var min = index == 0
                ? 0.0
                : (isLeftEdge ? Boundaries[index - 1].LeftY : Boundaries[index - 1].RightY) + minimumGap;

            var max = index == Boundaries.Count - 1
                ? ImageHeight
                : (isLeftEdge ? Boundaries[index + 1].LeftY : Boundaries[index + 1].RightY) - minimumGap;

            if (max < min)
            {
                return Math.Clamp((min + max) / 2.0, 0, ImageHeight);
            }

            return Math.Clamp(value, min, max);
        }

        private void ClampAllBoundaries()
        {
            for (var index = 0; index < Boundaries.Count; index++)
            {
                ClampBoundary(index);
            }
        }

        private bool TryGetBoundary(int index, out RowBoundary boundary)
        {
            boundary = null!;
            if (index < 0 || index >= Boundaries.Count)
            {
                return false;
            }

            boundary = Boundaries[index];
            return true;
        }

        private int _rowCount = 1;
        private double _imageWidth;
        private double _imageHeight;
    }
}
