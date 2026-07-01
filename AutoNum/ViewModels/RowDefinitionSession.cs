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

        private static Color GetPreviewColor(int row)
        {
            var palette = new[]
            {
                Color.FromArgb(255, 224, 242, 254),
                Color.FromArgb(255, 255, 244, 214),
                Color.FromArgb(255, 243, 229, 245),
                Color.FromArgb(255, 232, 245, 233)
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
