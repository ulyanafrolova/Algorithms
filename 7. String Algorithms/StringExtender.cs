using System;
using System.Text;

namespace Lab15
{
    public static class StringExtender
    {
        /// <summary>
        /// Returns the period of the string 's', i.e., the smallest positive number 'p' 
        /// such that s[i] = s[i+p] for every i from 0 to |s|-p-1.
        /// 
        /// Time complexity: O(|s|)
        /// </summary>
        /// <param name="s">The input string.</param>
        /// <returns>The period of the string, or 0 if the string is empty.</returns>
        public static int Period(this string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;

            int[] P = CountP(s);
            return s.Length - P[s.Length - 1];
        }

        /// <summary>
        /// Computes the Prefix (Pi) array for the KMP algorithm.
        /// </summary>
        public static int[] CountP(string s)
        {
            int n = s.Length;
            int[] P = new int[n];
            
            if (n == 0) return P;

            P[0] = 0;
            int t = 0;

            for (int i = 1; i < n; i++)
            {
                while (t > 0 && s[t] != s[i])
                {
                    t = P[t - 1];
                }
                if (s[t] == s[i])
                {
                    t++;
                }
                P[i] = t;
            }
            return P;
        }

        /// <summary>
        /// Determines the maximum power contained within the string 's'.
        /// 
        /// If 'x' is a word, then the k-th power of 'x' is defined as the word 'x' repeated 'k' times 
        /// (e.g., 'xyzxyzxyz' is the 3rd power of the word 'xyz').
        /// 
        /// The method returns the largest 'k' such that the k-th power of some word is contained 
        /// in 's' as a contiguous substring.
        /// 
        /// Time complexity: O(|s|^2)
        /// </summary>
        /// <param name="s">The input string.</param>
        /// <param name="startIndex">The starting index of the first character of the found power.</param>
        /// <param name="endIndex">The index of the first character immediately following the found power.</param>
        /// <returns>The maximum power 'k' found within the string.</returns>
        public static int MaxPower(this string s, out int startIndex, out int endIndex)
        {
            if (string.IsNullOrEmpty(s))
            {
                startIndex = -1;
                endIndex = -1;
                return 0;
            }

            int n = s.Length;
            int maxK = 1;
            
            // Default to the first character (power of 1)
            startIndex = 0;
            endIndex = 1;

            // Allocate the Pi array once outside the loop to avoid O(|s|^2) garbage collection overhead.
            int[] P = new int[n];

            // Iterate over all possible starting positions of our potential substring.
            for (int i = 0; i < n; i++)
            {
                // We represent the substring starting at index 'i'.
                // We calculate the Pi array implicitly on the string 's' offset by 'i'.
                int len = n - i;
                P[0] = 0;
                int t = 0;

                for (int j = 1; j < len; j++)
                {
                    // Compute the Pi array for the suffix starting at 'i'
                    while (t > 0 && s[i + t] != s[i + j])
                    {
                        t = P[t - 1];
                    }
                    
                    if (s[i + t] == s[i + j])
                    {
                        t++;
                    }
                    
                    P[j] = t;

                    // The length of the current considered substring is (j + 1)
                    int currentLen = j + 1;
                    
                    // The smallest period of the current substring is its length minus its longest proper prefix-suffix length
                    int period = currentLen - P[j];

                    // If the current length is a perfect multiple of the period, we have found a valid perfect power
                    if (currentLen % period == 0)
                    {
                        int currentK = currentLen / period;
                        
                        // If we found a strictly greater power, update our tracking variables
                        if (currentK > maxK)
                        {
                            maxK = currentK;
                            startIndex = i;
                            endIndex = i + currentLen;
                        }
                    }
                }
            }

            return maxK;
        }
    }
}
