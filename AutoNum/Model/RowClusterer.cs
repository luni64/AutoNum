using AutoNumber.ViewModels;

namespace AutoNumber.Model;

/// <summary>
/// Assigns freshly detected faces to rows and derives the slanted row boundaries stored in
/// metadata. Works on the LABEL anchor points (not the raw face rectangles) so the emitted
/// boundaries are guaranteed to agree with everything else in the app that resolves rows from
/// boundaries (row-edit mode, drag-across-boundary, reopen from metadata).
///
/// Pipeline:
///   1. Chain growth — link horizontally neighbouring labels of similar height (union-find),
///      the way text-line segmentation works. Local comparisons make this tolerant to tilted
///      or sagging rows and independent of row spacing; no up-front row-count guess.
///   2. Chain merge — rows broken by a gap (missed detection, person missing in the middle)
///      are rejoined when their fitted lines mutually predict each other's members.
///   3. Stragglers — a lone label (child in front, baby on a lap) joins the nearest row line
///      if it is close enough relative to its own face size, else it forms its own row.
///   4. Boundaries — between consecutive rows, the straight (slanted) line best separating the
///      two clusters is chosen (max margin when separable, min misclassifications otherwise).
///   5. Reassign — every label's final row is resolved from the boundaries, exactly like
///      row-edit mode and metadata load do, so "each label starts on its detected side" is an
///      invariant by construction, not a probability.
///
/// All tolerances are relative to the detected face heights, so the algorithm is scale-invariant.
/// </summary>
public static class RowClusterer
{
    #region Tuning constants

    /// <summary>How far left (× median face height) a label looks for a same-row neighbour.</summary>
    private const double LinkWindowFactor = 3.0;

    /// <summary>Max vertical offset (× the smaller face height of the pair) for two labels to link.</summary>
    private const double LinkToleranceFactor = 0.6;

    /// <summary>Max mutual median prediction error (× median face height) for two chains to merge.</summary>
    private const double MergeToleranceFactor = 0.5;

    /// <summary>
    /// Max horizontal gap (× median face height) between two chains' X-ranges for them to be
    /// merge candidates. Extrapolating a fitted line far beyond a chain's extent is meaningless
    /// and used to fuse unrelated stragglers on opposite sides of the photo into one "row".
    /// </summary>
    private const double MaxMergeGapFactor = 4.0;

    /// <summary>Max distance (× own face height) for a lone label to join an existing row's line.</summary>
    private const double StragglerToleranceFactor = 1.2;

    /// <summary>
    /// Looser link tolerance (× smaller face height of the pair) used as a last resort to pull a
    /// leftover singleton into a nearby chain instead of letting it spawn a one-person row —
    /// e.g. two kneeling children at noticeably different head heights in front of a group.
    /// </summary>
    private const double SingletonRescueFactor = 1.1;

    /// <summary>
    /// If the best straight boundary between two vertically adjacent rows still misclassifies
    /// more than this fraction of the smaller row, the clusters are not linearly separable and
    /// are treated as one row — the app's row model (straight slanted boundaries) is the final
    /// authority on what can count as separate rows.
    /// </summary>
    private const double NonSeparableFraction = 0.25;

    /// <summary>
    /// Minimum vertical spacing (× median face height) between two adjacent rows' median lines
    /// for them to count as separate rows. People in one physical row jitter vertically by up to
    /// about a head (children between adults, seated vs. slouching), while genuinely different
    /// rows sit at least a head apart — closer "rows" are stratification noise and get merged.
    /// </summary>
    private const double MinRowSpacingFactor = 1.0;

    /// <summary>Slope clamp for fitted row lines and boundaries (rows in photos are nearly level).</summary>
    private const double MaxRowSlope = 0.1;

    #endregion

    /// <summary>A label anchor point plus the height of the detected face it belongs to.</summary>
    public readonly record struct FacePoint(double X, double Y, double FaceHeight);

    public sealed record Result(int[] Rows, List<RowBoundary> Boundaries)
    {
        public int RowCount => Boundaries.Count + 1;
    }

    public static Result AssignRows(IReadOnlyList<FacePoint> points, double imageWidth, double imageHeight)
    {
        var n = points.Count;
        if (n == 0)
        {
            return new Result([], []);
        }

        var medianHeight = MedianFaceHeight(points, imageWidth, imageHeight);
        var heights = points.Select(p => p.FaceHeight > 0 ? p.FaceHeight : medianHeight).ToArray();

        if (n == 1)
        {
            return new Result([1], []);
        }

        var chains = GrowChains(points, heights, medianHeight);
        MergeBrokenChains(chains, points, medianHeight);
        AttachStragglers(chains, points, heights, medianHeight);
        RescueSingletons(chains, points, heights, medianHeight);

        // Order rows top to bottom by their members' mean Y.
        chains.Sort((a, b) => a.Average(i => points[i].Y).CompareTo(b.Average(i => points[i].Y)));

        // Row tilt comes from the camera/terrain and is shared by the whole photo, so all
        // boundaries use one global slope. Independently sloped boundaries can cross and carve
        // wedge-shaped regions that assign unrelated labels to the same row.
        var globalSlope = ComputeGlobalSlope(chains, points);

        MergeNonSeparableRows(chains, points, globalSlope, medianHeight);
        MergeCloseRows(chains, points, globalSlope, medianHeight);

        var rows = new int[n];
        if (chains.Count == 1)
        {
            Array.Fill(rows, 1);
            return new Result(rows, []);
        }

        var boundaries = FitBoundaries(chains, points, globalSlope, medianHeight, imageWidth, imageHeight);

        // Boundaries are the app-wide source of truth for row membership — resolve the final
        // assignment from them (and drop any boundary whose row ends up empty).
        while (true)
        {
            for (var i = 0; i < n; i++)
            {
                rows[i] = ResolveRowFromBoundaries(points[i], boundaries, imageWidth);
            }

            var emptyRow = FindEmptyRow(rows, boundaries.Count + 1);
            if (emptyRow < 0 || boundaries.Count == 0)
            {
                break;
            }

            boundaries.RemoveAt(Math.Min(emptyRow - 1, boundaries.Count - 1));
        }

        return new Result(rows, boundaries);
    }

    /// <summary>Resolves a point's row against boundary lines, same rule as ImageVM/row-edit mode.</summary>
    private static int ResolveRowFromBoundaries(FacePoint point, List<RowBoundary> boundaries, double imageWidth) =>
        1 + boundaries.Count(b => point.Y > b.GetYAtX(point.X, imageWidth));

    private static int FindEmptyRow(int[] rows, int rowCount)
    {
        for (var row = 1; row <= rowCount; row++)
        {
            if (!rows.Contains(row))
            {
                return row;
            }
        }

        return -1;
    }

    private static double MedianFaceHeight(IReadOnlyList<FacePoint> points, double imageWidth, double imageHeight)
    {
        var valid = points.Where(p => p.FaceHeight > 0).Select(p => p.FaceHeight).OrderBy(h => h).ToList();
        if (valid.Count == 0)
        {
            // No usable face sizes (e.g. manually placed labels) — fall back to a plausible
            // group-photo face size so the tolerances stay in a sane range.
            return Math.Max(1, 0.04 * Math.Max(imageWidth, imageHeight));
        }

        var mid = valid.Count / 2;
        return valid.Count % 2 == 1 ? valid[mid] : (valid[mid - 1] + valid[mid]) / 2.0;
    }

    #region Stage 1 — chain growth

    private static List<List<int>> GrowChains(IReadOnlyList<FacePoint> points, double[] heights, double medianHeight)
    {
        var n = points.Count;
        var parent = Enumerable.Range(0, n).ToArray();

        int Find(int i) => parent[i] == i ? i : parent[i] = Find(parent[i]);
        void Union(int i, int j) => parent[Find(i)] = Find(j);

        var byX = Enumerable.Range(0, n).OrderBy(i => points[i].X).ToArray();
        var window = LinkWindowFactor * medianHeight;

        for (var a = 0; a < n; a++)
        {
            for (var b = a + 1; b < n; b++)
            {
                var i = byX[a];
                var j = byX[b];
                if (points[j].X - points[i].X > window)
                {
                    break;
                }

                var tolerance = LinkToleranceFactor * Math.Min(heights[i], heights[j]);
                if (Math.Abs(points[j].Y - points[i].Y) < tolerance)
                {
                    Union(i, j);
                }
            }
        }

        return Enumerable.Range(0, n)
            .GroupBy(Find)
            .Select(group => group.ToList())
            .ToList();
    }

    #endregion

    #region Stage 2/3 — merge and stragglers

    /// <summary>Least-squares line y = a + b·x through a chain's points, slope clamped.</summary>
    private static (double A, double B) FitLine(List<int> members, IReadOnlyList<FacePoint> points)
    {
        var meanX = members.Average(i => points[i].X);
        var meanY = members.Average(i => points[i].Y);

        double varX = 0, cov = 0;
        foreach (var i in members)
        {
            var dx = points[i].X - meanX;
            varX += dx * dx;
            cov += dx * (points[i].Y - meanY);
        }

        var slope = varX < 1e-9 ? 0 : Math.Clamp(cov / varX, -MaxRowSlope, MaxRowSlope);
        return (meanY - slope * meanX, slope);
    }

    private static double MedianPredictionError(List<int> members, (double A, double B) fit, IReadOnlyList<FacePoint> points)
    {
        var errors = members
            .Select(i => Math.Abs(fit.A + fit.B * points[i].X - points[i].Y))
            .OrderBy(e => e)
            .ToList();

        var mid = errors.Count / 2;
        return errors.Count % 2 == 1 ? errors[mid] : (errors[mid - 1] + errors[mid]) / 2.0;
    }

    /// <summary>Horizontal gap between two chains' X-ranges (0 when they overlap).</summary>
    private static double HorizontalGap(List<int> a, List<int> b, IReadOnlyList<FacePoint> points)
    {
        var aMin = a.Min(i => points[i].X);
        var aMax = a.Max(i => points[i].X);
        var bMin = b.Min(i => points[i].X);
        var bMax = b.Max(i => points[i].X);
        return Math.Max(0, Math.Max(bMin - aMax, aMin - bMax));
    }

    private static void MergeBrokenChains(List<List<int>> chains, IReadOnlyList<FacePoint> points, double medianHeight)
    {
        var tolerance = MergeToleranceFactor * medianHeight;
        var maxGap = MaxMergeGapFactor * medianHeight;
        var merged = true;

        while (merged)
        {
            merged = false;

            for (var i = 0; i < chains.Count && !merged; i++)
            {
                var fitI = FitLine(chains[i], points);
                for (var j = i + 1; j < chains.Count; j++)
                {
                    if (HorizontalGap(chains[i], chains[j], points) > maxGap)
                    {
                        continue;
                    }

                    var fitJ = FitLine(chains[j], points);
                    if (MedianPredictionError(chains[j], fitI, points) < tolerance &&
                        MedianPredictionError(chains[i], fitJ, points) < tolerance)
                    {
                        chains[i].AddRange(chains[j]);
                        chains.RemoveAt(j);
                        merged = true;
                        break;
                    }
                }
            }
        }
    }

    private static void AttachStragglers(List<List<int>> chains, IReadOnlyList<FacePoint> points, double[] heights, double medianHeight)
    {
        var reach = LinkWindowFactor * medianHeight;
        var singletons = chains.Where(c => c.Count == 1).ToList();

        foreach (var singleton in singletons)
        {
            var index = singleton[0];
            var point = points[index];

            List<int>? bestChain = null;
            var bestDistance = double.MaxValue;

            foreach (var chain in chains)
            {
                if (chain.Count < 2)
                {
                    continue;
                }

                // Only judge against the chain's line where the line is actually supported by
                // members — far extrapolation says nothing about where the row really runs.
                if (point.X < chain.Min(i => points[i].X) - reach || point.X > chain.Max(i => points[i].X) + reach)
                {
                    continue;
                }

                var fit = FitLine(chain, points);
                var distance = Math.Abs(fit.A + fit.B * point.X - point.Y);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestChain = chain;
                }
            }

            if (bestChain is not null && bestDistance < StragglerToleranceFactor * heights[index])
            {
                bestChain.Add(index);
                chains.Remove(singleton);
            }
        }
    }

    /// <summary>
    /// Last-resort pass for singletons that survived <see cref="AttachStragglers"/>: link them to
    /// the chain containing their vertically closest member within the horizontal link window,
    /// at a looser tolerance than stage 1. Keeps e.g. two kneeling children at different head
    /// heights from becoming two one-person rows.
    /// </summary>
    private static void RescueSingletons(List<List<int>> chains, IReadOnlyList<FacePoint> points, double[] heights, double medianHeight)
    {
        var window = LinkWindowFactor * medianHeight;
        var changed = true;

        while (changed)
        {
            changed = false;

            foreach (var singleton in chains.Where(c => c.Count == 1).ToList())
            {
                var index = singleton[0];
                var point = points[index];

                List<int>? bestChain = null;
                var bestDeltaY = double.MaxValue;

                foreach (var chain in chains)
                {
                    if (ReferenceEquals(chain, singleton))
                    {
                        continue;
                    }

                    foreach (var member in chain)
                    {
                        if (Math.Abs(points[member].X - point.X) > window)
                        {
                            continue;
                        }

                        var deltaY = Math.Abs(points[member].Y - point.Y);
                        var tolerance = SingletonRescueFactor * Math.Min(heights[index], heights[member]);
                        if (deltaY < tolerance && deltaY < bestDeltaY)
                        {
                            bestDeltaY = deltaY;
                            bestChain = chain;
                        }
                    }
                }

                if (bestChain is not null)
                {
                    bestChain.Add(index);
                    chains.Remove(singleton);
                    changed = true;
                }
            }
        }
    }

    /// <summary>
    /// The single row-tilt slope shared by all boundaries: the size-weighted average of the
    /// multi-member chains' fitted slopes (singletons carry no slope information).
    /// </summary>
    private static double ComputeGlobalSlope(List<List<int>> chains, IReadOnlyList<FacePoint> points)
    {
        double weightedSum = 0;
        double weightTotal = 0;

        foreach (var chain in chains.Where(c => c.Count >= 2))
        {
            var fit = FitLine(chain, points);
            weightedSum += chain.Count * fit.B;
            weightTotal += chain.Count;
        }

        return weightTotal > 0 ? Math.Clamp(weightedSum / weightTotal, -MaxRowSlope, MaxRowSlope) : 0;
    }

    /// <summary>
    /// Merges vertically adjacent rows whose members no straight boundary can reasonably keep
    /// apart. The app models rows exclusively through straight slanted boundary lines, so two
    /// clusters that interleave beyond what such a line can express are one row by definition.
    /// Expects (and preserves) chains sorted top to bottom.
    /// </summary>
    private static void MergeNonSeparableRows(List<List<int>> chains, IReadOnlyList<FacePoint> points, double globalSlope, double medianHeight)
    {
        var k = 0;
        while (k < chains.Count - 1)
        {
            var upper = chains[k];
            var lower = chains[k + 1];
            var (_, errors, _) = BestThreshold(upper, lower, globalSlope, points, medianHeight);
            var allowed = Math.Max(1, (int)(NonSeparableFraction * Math.Min(upper.Count, lower.Count)));

            if (errors > allowed)
            {
                upper.AddRange(lower);
                chains.RemoveAt(k + 1);
                k = Math.Max(0, k - 1);
            }
            else
            {
                k++;
            }
        }
    }

    #endregion

    #region Stage 4 — boundary fitting

    /// <summary>
    /// Merges adjacent rows that sit closer together than a plausible row spacing. Distances are
    /// measured between the rows' median intercepts along the global slope; the closest pair is
    /// merged first and spacing re-evaluated until every remaining gap is plausible. Expects
    /// (and preserves) chains sorted top to bottom.
    /// </summary>
    private static void MergeCloseRows(List<List<int>> chains, IReadOnlyList<FacePoint> points, double globalSlope, double medianHeight)
    {
        var minSpacing = MinRowSpacingFactor * medianHeight;

        double MedianIntercept(List<int> chain)
        {
            var values = chain.Select(i => points[i].Y - globalSlope * points[i].X).OrderBy(c => c).ToList();
            var mid = values.Count / 2;
            return values.Count % 2 == 1 ? values[mid] : (values[mid - 1] + values[mid]) / 2.0;
        }

        while (chains.Count > 1)
        {
            var intercepts = chains.Select(MedianIntercept).ToList();

            var closest = -1;
            var closestGap = double.MaxValue;
            for (var k = 0; k < chains.Count - 1; k++)
            {
                var gap = intercepts[k + 1] - intercepts[k];
                if (gap < closestGap)
                {
                    closestGap = gap;
                    closest = k;
                }
            }

            if (closestGap >= minSpacing)
            {
                break;
            }

            chains[closest].AddRange(chains[closest + 1]);
            chains.RemoveAt(closest + 1);
        }
    }

    /// <summary>
    /// For each pair of vertically adjacent rows, finds the boundary line (at the shared global
    /// slope) that best separates the two clusters: zero misclassifications with maximum margin
    /// when the clusters are linearly separable, minimum misclassifications otherwise. Using one
    /// slope for every boundary keeps them parallel, so they can never cross inside the image.
    /// </summary>
    private static List<RowBoundary> FitBoundaries(
        List<List<int>> chains, IReadOnlyList<FacePoint> points, double globalSlope, double medianHeight, double imageWidth, double imageHeight)
    {
        var boundaries = new List<RowBoundary>();

        for (var k = 0; k < chains.Count - 1; k++)
        {
            var (bestIntercept, _, _) = BestThreshold(chains[k], chains[k + 1], globalSlope, points, medianHeight);

            var leftY = bestIntercept;
            var rightY = bestIntercept + globalSlope * imageWidth;

            // Keep boundaries inside the image and strictly ordered top to bottom.
            var minLeft = boundaries.Count > 0 ? boundaries[^1].LeftY + 2.0 : 0.0;
            var minRight = boundaries.Count > 0 ? boundaries[^1].RightY + 2.0 : 0.0;
            leftY = Math.Clamp(leftY, minLeft, imageHeight);
            rightY = Math.Clamp(rightY, minRight, imageHeight);

            boundaries.Add(new RowBoundary(leftY, rightY));
        }

        return boundaries;
    }

    /// <summary>
    /// 1D separation for a fixed slope: projects every member to its intercept c = y − slope·x
    /// and picks the threshold with the fewest points on the wrong side (upper row above,
    /// lower row below), tie-broken by the widest gap.
    /// </summary>
    private static (double Intercept, int Errors, double Gap) BestThreshold(
        List<int> upper, List<int> lower, double slope, IReadOnlyList<FacePoint> points, double medianHeight)
    {
        var items = upper.Select(i => (C: points[i].Y - slope * points[i].X, IsUpper: true))
            .Concat(lower.Select(i => (C: points[i].Y - slope * points[i].X, IsUpper: false)))
            .OrderBy(item => item.C)
            .ToList();

        // lowerBelow[k] = lower-row members among the first k items (they'd land above the
        // threshold, i.e. misclassified); upperAbove[k] = upper-row members after the first k.
        var total = items.Count;
        var upperTotal = upper.Count;

        var bestErrors = int.MaxValue;
        var bestGap = double.MinValue;
        var bestIntercept = 0.0;

        var lowerSeen = 0;
        var upperSeen = 0;

        for (var k = 0; k <= total; k++)
        {
            var errors = lowerSeen + (upperTotal - upperSeen);
            var gap = k switch
            {
                0 => 0.0,
                _ when k == total => 0.0,
                _ => items[k].C - items[k - 1].C
            };

            var intercept = k switch
            {
                0 => items[0].C - medianHeight / 2.0,
                _ when k == total => items[^1].C + medianHeight / 2.0,
                _ => (items[k - 1].C + items[k].C) / 2.0
            };

            // A zero-width gap means identical projections on both sides — not a usable split.
            var usable = k == 0 || k == total || gap > 0;
            if (usable && (errors < bestErrors || (errors == bestErrors && gap > bestGap)))
            {
                bestErrors = errors;
                bestGap = gap;
                bestIntercept = intercept;
            }

            if (k < total)
            {
                if (items[k].IsUpper)
                {
                    upperSeen++;
                }
                else
                {
                    lowerSeen++;
                }
            }
        }

        return (bestIntercept, bestErrors, bestGap);
    }

    #endregion
}
