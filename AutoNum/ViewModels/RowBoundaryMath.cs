namespace AutoNumber.ViewModels
{
    /// <summary>
    /// The single home for the two pieces of row-boundary geometry that used to be duplicated
    /// across LabelManager, ImageVM, RowDefinitionManager/-Session, PictureDisplay and
    /// RowClusterer: resolving which row a point falls into, and deriving midline boundaries
    /// from already-assigned rows. Lives next to <see cref="RowBoundary"/> and should move to
    /// Model together with it.
    /// </summary>
    public static class RowBoundaryMath
    {
        /// <summary>
        /// Row (1-based) of a point: 1 plus the number of boundaries the point lies below.
        /// The same rule everywhere — metadata load, row-edit mode, drag preview, detection.
        /// </summary>
        public static int ResolveRow(double x, double y, IEnumerable<RowBoundary> boundaries, double imageWidth)
        {
            var row = 1;
            foreach (var boundary in boundaries)
            {
                if (y > boundary.GetYAtX(x, imageWidth))
                {
                    row++;
                }
            }

            return row;
        }

        /// <summary>
        /// Overload for callers that track boundary endpoints outside of
        /// <see cref="RowBoundary"/> instances (e.g. drag visuals in PictureDisplay).
        /// </summary>
        public static int ResolveRow(double x, double y, IEnumerable<(double LeftY, double RightY)> boundaries, double imageWidth)
        {
            var row = 1;
            foreach (var (leftY, rightY) in boundaries)
            {
                var boundaryY = imageWidth <= 0
                    ? leftY
                    : leftY + (rightY - leftY) * Math.Clamp(x / imageWidth, 0.0, 1.0);
                if (y > boundaryY)
                {
                    row++;
                }
            }

            return row;
        }

        /// <summary>
        /// Builds horizontal boundaries between already-assigned rows: each boundary sits midway
        /// between one row's lowest anchor and the next row's highest anchor, clamped to stay
        /// ordered and inside the image. Anchors with Row &lt;= 0 are ignored; returns an empty
        /// list when fewer than two rows exist. Row count follows as boundaries.Count + 1.
        /// </summary>
        public static List<RowBoundary> MidlineBoundaries(IEnumerable<(int Row, double Y)> anchors, double imageHeight)
        {
            var groups = anchors
                .Where(anchor => anchor.Row > 0)
                .GroupBy(anchor => anchor.Row)
                .OrderBy(group => group.Key)
                .Select(group => (MinY: group.Min(a => a.Y), MaxY: group.Max(a => a.Y)))
                .ToList();

            var boundaries = new List<RowBoundary>();
            if (groups.Count <= 1)
            {
                return boundaries;
            }

            for (var index = 0; index < groups.Count - 1; index++)
            {
                var boundaryY = (groups[index].MaxY + groups[index + 1].MinY) / 2.0;
                var minAllowed = index == 0 ? 0.0 : boundaries[index - 1].LeftY + 2.0;
                boundaryY = Math.Clamp(boundaryY, minAllowed, imageHeight);
                boundaries.Add(new RowBoundary(boundaryY, boundaryY));
            }

            return boundaries;
        }
    }
}
