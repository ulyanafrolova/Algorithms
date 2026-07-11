using System;
using System.Collections.Generic;
using System.Linq;

namespace ASD
{
    public class ConvexHullCalculator : System.MarshalByRefObject
    {
        // 2D Cross product (determinant)
        // Computes the Z-coordinate of the cross product of vectors OA and OB.
        // Formula used: $(a_x - o_x)(b_y - o_y) - (a_y - o_y)(b_x - o_x)$
        private int Cross((double, double) o, (double, double) a, (double, double) b)
        {
            double value = (a.Item1 - o.Item1) * (b.Item2 - o.Item2) - (a.Item2 - o.Item2) * (b.Item1 - o.Item1);
            return Math.Abs(value) < 1e-10 ? 0 : value < 0 ? -1 : 1;
        }

        /// <summary>
        /// Computes the convex hull of a given set of points using the Monotone Chain algorithm.
        /// </summary>
        /// <param name="points">An array of 2D points.</param>
        /// <returns>An array of points representing the vertices of the convex hull in counter-clockwise order.</returns>
        public (double, double)[] ConvexHull((double, double)[] points)
        {
            if (points == null || points.Length <= 1) return points;
            
            // Sort points lexicographically (first by X, then by Y)
            var sortedPoints = points.OrderBy(p => p.Item1).ThenBy(p => p.Item2).ToArray();

            // Build the lower hull
            var lower = new List<(double, double)>();
            foreach (var p in sortedPoints)
            {
                while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0)
                {
                    lower.RemoveAt(lower.Count - 1);
                }
                lower.Add(p);
            }

            // Build the upper hull
            var upper = new List<(double, double)>();
            for (int i = sortedPoints.Length - 1; i >= 0; --i)
            {
                var p = sortedPoints[i];
                while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], p) <= 0)
                {
                    upper.RemoveAt(upper.Count - 1);
                }
                upper.Add(p);
            }

            // The last point of each half is the first point of the other, so we remove them to avoid duplicates
            lower.RemoveAt(lower.Count - 1);
            upper.RemoveAt(upper.Count - 1);

            // Concatenate lower and upper hulls to form the full convex hull
            lower.AddRange(upper);
            return lower.ToArray();
        }

        /// <summary>
        /// Computes the convex hull of two existing convex polygons.
        /// This is done efficiently by splitting both polygons into upper and lower hulls, 
        /// merging them, and rebuilding the final bounds.
        /// </summary>
        /// <param name="poly1">Vertices of the first convex polygon.</param>
        /// <param name="poly2">Vertices of the second convex polygon.</param>
        /// <returns>Vertices of the merged convex hull.</returns>
        public (double, double)[] ConvexHullOfTwo((double, double)[] poly1, (double, double)[] poly2)
        {
            var (lower1, upper1) = FindUpperLower(poly1);
            var (lower2, upper2) = FindUpperLower(poly2);

            var lower = Merge(lower1, lower2);
            var upper = Merge(upper1, upper2);

            var finalLower = Rebuild(lower);
            var finalUpper = Rebuild(upper);

            // Remove duplicates at the connection points
            finalLower.RemoveAt(finalLower.Count - 1);
            finalUpper.RemoveAt(finalUpper.Count - 1);

            finalLower.AddRange(finalUpper);
            return finalLower.ToArray();
        }

        /// <summary>
        /// Splits a given convex polygon into its lower and upper hulls based on the leftmost and rightmost points.
        /// </summary>
        public (List<(double, double)>, List<(double, double)>) FindUpperLower((double, double)[] poly)
        {
            int n = poly.Length;
            int minIdx = 0;
            int maxIdx = 0;
            
            // Find the leftmost (minIdx) and rightmost (maxIdx) points
            for (int i = 0; i < n; ++i)
            {
                if (poly[i].Item1 < poly[minIdx].Item1 || (poly[i].Item1 == poly[minIdx].Item1 && poly[i].Item2 < poly[minIdx].Item2))
                {
                    minIdx = i;
                }
                if (poly[i].Item1 > poly[maxIdx].Item1 || (poly[i].Item1 == poly[maxIdx].Item1 && poly[i].Item2 > poly[maxIdx].Item2))
                {
                    maxIdx = i;
                }
            }

            // Extract the lower hull
            var lower = new List<(double, double)>();
            int currLower = minIdx;
            while (true)
            {
                lower.Add(poly[currLower]);
                if (currLower == maxIdx) break;
                currLower = (currLower + 1) % n;
            }

            // Extract the upper hull
            var upper = new List<(double, double)>();
            int currUpper = maxIdx;
            while (true)
            {
                upper.Add(poly[currUpper]);
                if (currUpper == minIdx) break;
                currUpper = (currUpper + 1) % n;
            }
            return (lower, upper);
        }

        /// <summary>
        /// Merges two monotonically sorted chains (either lower or upper hulls) into a single sorted list.
        /// </summary>
        public List<(double, double)> Merge(List<(double, double)> first, List<(double, double)> second) 
        {
            var result = new List<(double, double)>(first.Count + second.Count);
            int i = 0, j = 0;

            // Determine the sorting direction. 
            // Lower hulls go from the smallest X to the largest.
            // Upper hulls go from the largest X to the smallest.
            bool isAscending = true;
            if (first.Count > 1)
            {
                isAscending = first[0].Item1 < first[first.Count - 1].Item1 || 
                              (first[0].Item1 == first[first.Count - 1].Item1 && first[0].Item2 < first[first.Count - 1].Item2);
            }
            else if (second.Count > 1)
            {
                isAscending = second[0].Item1 < second[second.Count - 1].Item1 || 
                              (second[0].Item1 == second[second.Count - 1].Item1 && second[0].Item2 < second[second.Count - 1].Item2);
            }

            // Standard two-pointer merge approach
            while (i < first.Count && j < second.Count)
            {
                bool takeFirst;
                if (isAscending)
                {
                    takeFirst = first[i].Item1 < second[j].Item1 || 
                                (first[i].Item1 == second[j].Item1 && first[i].Item2 <= second[j].Item2);
                }
                else
                {
                    takeFirst = first[i].Item1 > second[j].Item1 || 
                                (first[i].Item1 == second[j].Item1 && first[i].Item2 >= second[j].Item2);
                }

                if (takeFirst) result.Add(first[i++]);
                else result.Add(second[j++]);
            }

            // Append any remaining elements
            while (i < first.Count) result.Add(first[i++]);
            while (j < second.Count) result.Add(second[j++]);

            return result;
        }

        /// <summary>
        /// Rebuilds a valid convex chain by removing concave sections, collinear points, and duplicates.
        /// </summary>
        public List<(double, double)> Rebuild(List<(double, double)> chain) 
        {
            var result = new List<(double, double)>();
            foreach (var p in chain)
            {
                // As long as the last 3 points do not form a strictly left turn, we remove the middle one.
                // This ensures the resulting chain remains purely convex and eliminates overlaps/collinearities.
                while (result.Count >= 2 && Cross(result[result.Count - 2], result[result.Count - 1], p) <= 0)
                {
                    result.RemoveAt(result.Count - 1);
                }
                result.Add(p);
            }
            return result;
        }
    }
}
