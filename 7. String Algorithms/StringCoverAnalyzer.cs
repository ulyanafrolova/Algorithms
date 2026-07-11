using System;
using System.Collections.Generic;

namespace ASD
{
    public class StringCoverAnalyzer : MarshalByRefObject
    {
        /// <summary>
        /// Finds the longest substring of the text that can be fully covered by overlapping occurrences of the pattern.
        /// </summary>
        /// <param name="text">Input text string.</param>
        /// <param name="pattern">Pattern used to find the longest covered substring within the `text`.</param>
        /// <param name="result">The longest substring of `text` covered by `pattern`.</param>
        /// <returns>The length of the longest substring of `text` covered by `pattern`.</returns>
        public int FindLongestCoveredSubstring(string text, string pattern, out string result)
        {
            // Algorithm: 
            // 1. Create the Pi array (prefix function).
            // 2. Run KMP to find all starting indices where the pattern occurs in the text.
            // 3. Save these starting indices into the 'candidates' list.
            // 4. Iterate through the list from left to right and merge overlapping segments.
            // 5. If a newly found pattern starts before the previous one ends, it means they overlap and can be merged.
            // 6. If the new pattern starts too late (a gap exists), we finalize the current block, check 
            //    if it's the new maxLen, and start a new block from scratch.

            int n = text.Length;
            int m = pattern.Length;
            int[] pi = ComputeP(pattern);
            List<int> candidates = new List<int>();

            int q = 0;
            for (int i = 0; i < n; i++)
            {
                while (q > 0 && pattern[q] != text[i])
                    q = pi[q - 1];

                if (pattern[q] == text[i])
                    q++;

                if (q == m)
                {
                    candidates.Add(i - m + 1);
                    q = pi[q - 1];
                }
            }

            if (candidates.Count == 0)
            {
                result = "";
                return 0;
            }
            int maxLen = 0;
            int bestStart = -1;

            int currentStart = candidates[0];
            int currentEnd = candidates[0] + m;

            for (int i = 1; i < candidates.Count; i++)
            {
                if (candidates[i] <= currentEnd)
                {
                    currentEnd = candidates[i] + m;
                }
                else
                {
                    int len = currentEnd - currentStart;
                    if (len > maxLen)
                    {
                        maxLen = len;
                        bestStart = currentStart;
                    }

                    currentStart = candidates[i];
                    currentEnd = candidates[i] + m;
                }
            }
            int resultLen = currentEnd - currentStart;
            if (resultLen > maxLen)
            {
                maxLen = resultLen;
                bestStart = currentStart;
            }

            result = text.Substring(bestStart, maxLen);
            return maxLen;
        }

        private int[] ComputeP(string text)
        {
            int n = text.Length;
            int[] P = new int[n];
            int j = 0;
            for (int i = 1; i < n; i++)
            {
                while (j > 0 && text[j] != text[i]) j = P[j - 1];
                if (text[j] == text[i]) j++;
                P[i] = j;
            }
            return P;
        }

        /// <summary>
        /// Finds the shortest substring (prefix) that fully covers the input word.
        /// </summary>
        /// <param name="word">Input word.</param>
        /// <param name="result">The shortest covering substring of `word`.</param>
        /// <returns>The length of the shortest covering substring.</returns>
        public int FindShortestWordCover(string word, out string result)
        {
            // Algorithm description:
            // 1. Time complexity must be O(n), so we use dynamic programming to avoid nested loops.
            // 2. S[i] represents the length of the shortest word that covers a prefix of length i.
            // 3. maxReach[j] represents how far into the word a cover of length j can reach.
            // 4. By default, every prefix covers itself, so S[i] = i, maxReach[i] = i.
            // 5. Compute the Pi array.
            // 6. Check the Pi array to see if the current prefix has a longest proper prefix-suffix.
            // 7. If it does, we check its shortest possible cover (from the S array).
            // 8. We check if the current cover reach touches the start of its last occurrence in this prefix,
            //    meaning maxReach[j] >= i - j. 
            // 9. If so, it means this smaller word can cover the entire current prefix.
            // 10. Update S[i] and extend maxReach.

            int n = word.Length;
            int[] pi = ComputeP(word);
            int[] S = new int[n + 1];
            int[] maxReach = new int[n + 1];
            for (int i = 1; i <= n; i++)
            {
                S[i] = i;
                maxReach[i] = i;

                int p_len = pi[i - 1];
                if (p_len > 0)
                {
                    int j = S[p_len];
                    if (maxReach[j] >= i - j)
                    {
                        S[i] = j;
                        maxReach[j] = i;
                    }
                }
            }

            int shortestLen = S[n];
            result = word.Substring(0, shortestLen);
            return shortestLen;
        }
    }
}
