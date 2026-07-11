using System;
using System.Collections.Generic;
using ASD.Graphs;

namespace ASD
{
    public class EmpiricalDistributionAnalyzer : MarshalByRefObject
    {
        /// <summary>
        /// Calculates the Kolmogorov-Smirnov statistic for two non-decreasingly 
        /// ordered samples.
        ///
        /// The solution runs in $O(n_1 + n_2)$ time,
        /// where $n_1$ and $n_2$ are the lengths of the respective samples.
        /// </summary>
        /// <param name="sample1">First sample, sorted in non-decreasing order.</param>
        /// <param name="sample2">Second sample, sorted in non-decreasing order.</param>
        /// <returns>
        /// The value of the statistic $D = \sup_x |F_1(x) - F_2(x)|$.
        /// </returns>
        public double ComputeTwoSampleStatistic(double[] sample1, double[] sample2)
        {
            int n1 = sample1.Length;
            int n2 = sample2.Length;
            int i = 0, j = 0;

            double maxD = 0.0;

            while (i < n1 || j < n2)
            {
                double p1 = double.MaxValue, p2 = double.MaxValue;
                if (i < n1) p1 = sample1[i];
                if (j < n2) p2 = sample2[j];

                double min = Math.Min(p1, p2);
                while (i < n1 && sample1[i] == min) i++;
                while (j < n2 && sample2[j] == min) j++;

                double F1 = (double)i / n1;
                double F2 = (double)j / n2;

                double D = Math.Abs(F1 - F2);
                if (D > maxD) maxD = D;
            }

            return maxD;
        }

        /// <summary>
        /// Calculates the maximum distance between the values of any two empirical 
        /// cumulative distribution functions (ECDF) at the same point $x$, for $K$ samples.
        ///
        /// The solution was initially expected to run in $O(nK)$ time, where $n$ is the total 
        /// length of all samples, and $K$ is the number of samples.
        /// </summary>
        /// <param name="samples">
        /// An array of $K$ samples. For $i = 0, 1, ..., K - 1$, the sample samples[i]
        /// has length $n_i$ and is sorted in non-decreasing order.
        /// </param>
        /// <returns>
        /// The value of the statistic $D = \sup_x (\max_i F_i(x) - \min_j F_j(x))$.
        /// </returns>
        public double ComputeMultiSampleStatistic(double[][] samples)
        {
            // Algorithm:
            // 1. We initialize two structures: a priority queue (stores the smallest incoming $x$ values) 
            //    and a SortedSet (stores the current state of the ECDFs for each sample).
            // 2. In each step, we extract the smallest value across all $K$ samples.
            // 3. We find all samples that reach the minimum value at this given moment.
            // 4. For these samples, we shift their index and update their 'currentF' values.
            // 5. Having updated 'currentF' for a given $x$, we take the maximum and minimum $F$ values 
            //    from 'currentF' and update the global maximum difference.

            // The entire while loop and inner loops shifting the pointers will iterate through each 
            // of the $n$ elements exactly once.
            // When processing an element, adding/removing operations from the SortedSet take $O(\log K)$ time 
            // (since the structures contain at most $K$ elements).
            // Therefore, the time complexity is: $n \times O(\log K) = O(n \log K)$.
            int k = samples.Length;
            double maxD = 0.0;

            // indexes[i] - the index of the currently considered element in the i-th sample
            int[] indexes = new int[k];
            // F[i] - the current value of the empirical cumulative distribution function for the i-th sample
            double[] F = new double[k];

            PriorityQueue<double, int> pq = new PriorityQueue<double, int>(k);
            SortedSet<(double F, int Id)> currentF = new SortedSet<(double F, int Id)>();

            for (int i = 0; i < k; i++)
            {
                pq.Insert(i, samples[i][0]);
                currentF.Add((0.0, i));
            }

            List<int> updatedArrays = new List<int>(k);

            while (pq.Count > 0)
            {
                int minId = pq.Peek();
                double min = samples[minId][indexes[minId]];
                
                updatedArrays.Clear();

                while (pq.Count > 0)
                {
                    int peekId = pq.Peek();
                    double peekVal = samples[peekId][indexes[peekId]];

                    if (peekVal == min)
                    {
                        updatedArrays.Add(pq.Extract());
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (int id in updatedArrays)
                {
                    currentF.Remove((F[id], id));

                    while (indexes[id] < samples[id].Length && samples[id][indexes[id]] == min)
                    {
                        indexes[id]++;
                    }

                    F[id] = (double)indexes[id] / samples[id].Length;
                    currentF.Add((F[id], id));

                    if (indexes[id] < samples[id].Length)
                    {
                        pq.Insert(id, samples[id][indexes[id]]);
                    }
                }
                double D = currentF.Max.F - currentF.Min.F;
                if (D > maxD) maxD = D;
            }
            return maxD;
        }

        /// <summary>
        /// Calculates the same statistic as ComputeMultiSampleStatistic, but strictly requires 
        /// the optimized time complexity.
        ///
        /// The solution runs in $O(n \log K)$ time, where $n$ is the total length of
        /// all samples, and $K$ is the number of samples.
        /// </summary>
        /// <param name="samples">
        /// An array of $K$ samples. For $i = 0, 1, ..., K - 1$, the sample samples[i]
        /// has length $n_i$ and is sorted in non-decreasing order.
        /// </param>
        /// <returns>
        /// The value of the statistic $D = \sup_x (\max_i F_i(x) - \min_j F_j(x))$.
        /// </returns>
        public double ComputeMultiSampleStatisticOptimized(double[][] samples)
        {
            // We reuse the logic because our initial implementation already achieved O(n log K) complexity.
            return ComputeMultiSampleStatistic(samples);
        }
    }
}
