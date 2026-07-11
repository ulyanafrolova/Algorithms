using System;
using System.Collections.Generic;
using ASD;
using ASD.Graphs;

namespace ASD2
{
    /// <summary>
    /// Finds the coloring of a given graph 'g' using the smallest possible number of colors.
    /// </summary>
    /// <param name="g">Graph (undirected)</param>
    /// <returns>Number of colors used and the coloring array (coloring[i] is the color of vertex i). Colors can be any integers.</returns>
    public class GraphColorer : MarshalByRefObject
    {
        // Algorithm:
        // 1. Finds the worst acceptable result (upper bound)
        // 2. Then finds the minimum (lower bound) 
        // 3. Goes down and searches for the perfect solution
        // Worst-case time complexity: O(K^n)
        public (int numberOfColors, int[] coloring) FindBestColoring(Graph g)
        {
            int n = g.VertexCount;
            // Edge case: if the graph has no vertices, there is nothing to color.
            if (n == 0) return (0, new int[0]);

            // 1. Create an adjacency list as an array of arrays (int[][])
            int[][] adj = new int[n][];
            for (int i = 0; i < n; i++)
            {
                var neighbors = new List<int>();
                foreach (var j in g.OutNeighbors(i))
                {
                    // Ignore self-loops 
                    if (j != i) neighbors.Add(j);
                }
                adj[i] = neighbors.ToArray();

                // Sort the neighbor list of each vertex.
                // This allows us to use binary search later to speed up the algorithm.
                Array.Sort(adj[i]);
            }

            // Upper bound
            int[] greedyColor = GreedyUpperBound(n, adj, out int greedyK);

            // Save this result as the best so far
            int bestK = greedyK;
            int[] bestColoring = greedyColor;

            // Lower bound 
            int lowerBound = GreedyLowerBound(n, adj);

            // We now have a range to check 
            for (int k = bestK - 1; k >= lowerBound; k--)
            {
                // Try to color the graph with k colors 
                if (TryK(k, n, adj, out int[] found))
                {
                    bestK = k;
                    bestColoring = found;
                }
                else
                {
                    // Since TryK proved that the graph cannot be colored with k colors,
                    // it definitely cannot be colored with k-1, k-2, etc., so the last result k+1
                    // was optimal.
                    break;
                }
            }
            return (bestK, bestColoring);
        }

        // Method implements a greedy algorithm.
        // Finds an upper bound, not the minimal coloring.
        // Worst-case time complexity: O(n^2)
        private int[] GreedyUpperBound(int n, int[][] adj, out int numColors)
        {
            // Create an 'order' array to hold vertex indices (from 0 to n-1)
            int[] order = new int[n];
            for (int i = 0; i < n; i++)
            {
                order[i] = i;
            }

            // Sort vertices in descending order by the number of their neighbors (vertex degree).
            // We do this because we want to color the most difficult vertices first.
            // The most difficult vertices are those with many neighbors, as they quickly run out 
            // of free colors, whereas a vertex with 1 neighbor can be colored at any time.
            Array.Sort(order, (a, b) => adj[b].Length.CompareTo(adj[a].Length));

            // color[i] - color for vertex i
            // -1 if not colored yet
            int[] color = new int[n];
            for (int i = 0; i < n; i++) color[i] = -1;

            // How many unique colors we have already used
            numColors = 0;

            // Auxiliary array to check which colors are already taken by neighbors
            bool[] used = new bool[n];

            // Greedy coloring 
            foreach (int v in order)
            {
                // Clear the auxiliary array up to index numColors+1, as we couldn't 
                // have used more colors
                Array.Clear(used, 0, numColors + 1);

                // Iterate through all neighbors of the current vertex v
                foreach (int u in adj[v])
                {
                    // If neighbor u is already colored, mark its color as used
                    if (color[u] >= 0) used[color[u]] = true;
                }

                // Find the smallest available (free) color
                int c = 0;
                while (used[c]) c++;

                // Color our vertex with the found color
                color[v] = c;

                // Update the number of used colors.
                // Since colors are 0-indexed, using color c means we have c+1 colors.
                if (c + 1 > numColors)
                {
                    numColors = c + 1;
                }
            }
            return color;
        }

        // Method finds the lower bound.
        // Finds the largest clique using a greedy method.
        // Worst-case time complexity: O(n^3 log n)
        private int GreedyLowerBound(int n, int[][] adj)
        {
            // Checking order 
            int[] order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;

            // Similarly to the upper bound, sort vertices in descending order by their degree.
            // Vertices with the most connections have the highest chance to form huge cliques, 
            // so we check them first.
            Array.Sort(order, (a, b) => adj[b].Length.CompareTo(adj[a].Length));

            // Size of the largest found clique.
            // Initialized to 1, as a single vertex is a clique of size 1 by itself.
            int best = 1;

            List<int> clique = new List<int>(n);

            // Try to build a clique around each vertex
            foreach (int start in order)
            {
                // If our vertex has e.g. 3 neighbors, the maximum clique it can form 
                // (itself + neighbors) will be 4.
                // So if we already found a clique of size 5, this vertex cannot exceed 'best'.
                // Since the array is sorted descending, no subsequent vertex will do this either, 
                // so we can stop searching.
                if (adj[start].Length + 1 <= best) break;

                clique.Clear();
                clique.Add(start);

                // Iterate through neighbors of the starting vertex 
                foreach (int v in adj[start])
                {
                    bool adjacent = true;

                    // Check if v is connected to every vertex we've already added to our clique
                    foreach (int u in clique)
                    {
                        // We don't need to check connection between v and 'start', 
                        // because we know they are connected.
                        if (u == start) continue;

                        // Use BinarySearch to speed up the algorithm.
                        // If BinarySearch returns a result < 0, it means v is not connected to u.
                        if (Array.BinarySearch(adj[v], u) < 0)
                        {
                            adjacent = false;
                            break;
                        }
                    }
                    // If v is connected to all, add it to the clique
                    if (adjacent) clique.Add(v);
                }
                if (clique.Count > best) best = clique.Count;
            }
            return best;
        }

        /// <summary>
        /// Method tries to color the graph using K colors.
        /// </summary>
        private bool TryK(int K, int n, int[][] adj, out int[] foundColoring)
        {
            foundColoring = new int[0];

            // color[i] - color of vertex i
            // -1 - no color
            int[] color = new int[n];
            for (int i = 0; i < n; i++) color[i] = -1;

            // neighborColors[i] - number of different colors among neighbors of vertex i
            int[] neighborColors = new int[n];

            // uncoloredNeighbors[i] - number of uncolored neighbors vertex i has
            int[] uncoloredNeighbors = new int[n];
            for (int i = 0; i < n; i++) uncoloredNeighbors[i] = adj[i].Length;

            // neighborColorCount[v * K + c] - how many neighbors of vertex v already have color c
            int[] neighborColorCount = new int[n * K];

            // temporarilyDeleted[i] - whether the vertex was temporarily deleted (postponed)
            bool[] temporarilyDeleted = new bool[n];
            // Stack to keep postponed vertices
            var temporarilyDeletedStack = new ASD.Stack<int>();
            // Vertices waiting for a color
            int uncoloredCount = n;

            // Since standard recursion caused a StackOverflowException on Test: Very Large Tree,
            // we implement an artificial stack using arrays.

            // pathV[level] - which vertex we are coloring at 'level'
            int[] pathV = new int[n];
            // pathC[level] - color number we are checking at 'level'
            int[] pathC = new int[n];
            // pathColorLimit[level] - maximum allowed color number at 'level'     
            int[] pathColorLimit = new int[n];
            // pathTempDeletedCount[level] - number of vertices we postponed at 'level'
            int[] pathTempDeletedCount = new int[n];
            // pathMaxColor[level] - highest color number used in the entire graph before descending to 'level'
            int[] pathMaxColor = new int[n + 1];
            // algorithm state: 0 - searching for a vertex to color, 1 - searching for a color for the selected vertex
            int[] state = new int[n + 1];

            pathMaxColor[0] = -1;
            // Current recursion level
            int level = 0;

            Queue<int> ToTemporarilyDelete = new Queue<int>();

            // Main loop simulating recursion.
            // If level drops below 0, it means we checked all options.
            while (level >= 0)
            {
                // State 0: Select a vertex to color or postpone
                if (state[level] == 0)
                {
                    int deletedThisLevel = 0;

                    // If a vertex has more free colors than uncolored neighbors, 
                    // we push it to the queue of easy vertices that we can postpone for later 
                    for (int v = 0; v < n; v++)
                    {
                        if (color[v] == -1 && !temporarilyDeleted[v])
                        {
                            if ((K - neighborColors[v]) > uncoloredNeighbors[v])
                            {
                                temporarilyDeleted[v] = true;
                                ToTemporarilyDelete.Enqueue(v);
                            }
                        }
                    }

                    // Extracting easy vertices might cause their neighbors to become easy too, 
                    // so we must repeat this until there are no easy vertices left to postpone.
                    while (ToTemporarilyDelete.Count > 0)
                    {
                        int v = ToTemporarilyDelete.Dequeue();
                        temporarilyDeletedStack.Push(v);
                        deletedThisLevel++;
                        uncoloredCount--;

                        foreach (int u in adj[v])
                        {
                            if (color[u] == -1 && !temporarilyDeleted[u])
                            {
                                uncoloredNeighbors[u]--;
                                // If this made the neighbor easy, add it to the queue
                                if ((K - neighborColors[u]) > uncoloredNeighbors[u])
                                {
                                    temporarilyDeleted[u] = true;
                                    ToTemporarilyDelete.Enqueue(u);
                                }
                            }
                        }
                    }
                    // Save how many vertices we postponed at this level, so we can undo this in case of failure
                    pathTempDeletedCount[level] = deletedThisLevel;

                    // If after postponements (or normal colorings) there are no uncolored vertices left, 
                    // we have a solution
                    if (uncoloredCount == 0)
                    {
                        int[] result = new int[n];
                        Array.Copy(color, result, n);

                        // Now we color the vertices we postponed on the stack. 
                        // The formula from step 1 guarantees we will find a free color for each vertex here.
                        foreach (int v in temporarilyDeletedStack)
                        {
                            bool[] used = new bool[K];
                            foreach (int u in adj[v])
                                if (result[u] >= 0 && result[u] < K) used[result[u]] = true;

                            int c = 0;
                            while (c < K && used[c]) c++;
                            result[v] = c;
                        }

                        foundColoring = result;
                        return true;
                    }

                    // Now we select the most difficult vertex, i.e., the one with 
                    // the fewest available colors for itself.
                    int bestV = -1, best = -1, bestDeg = -1;
                    for (int v = 0; v < n; v++)
                    {
                        if (color[v] != -1 || temporarilyDeleted[v]) continue;
                        if (neighborColors[v] > best ||
                            (neighborColors[v] == best && uncoloredNeighbors[v] > bestDeg))
                        {
                            best = neighborColors[v];
                            bestDeg = uncoloredNeighbors[v];
                            bestV = v;
                        }
                    }

                    pathV[level] = bestV;

                    // If we have only used colors up to e.g. 2 so far, it makes no sense to color a vertex with 4 or 5. 
                    // It's enough to let it take the colors used so far + one new color.
                    pathColorLimit[level] = Math.Min(K - 1, pathMaxColor[level] + 1);

                    // Save data to algorithm state 1
                    pathC[level] = 0;
                    state[level] = 1;
                }

                // State 1: Try to assign a specific color to the vertex
                else
                {
                    // Color we want to apply now
                    int c = pathC[level];
                    // Vertex we are painting
                    int bestV = pathV[level];

                    // As long as we haven't exhausted the available number of colors for this level
                    if (c <= pathColorLimit[level])
                    {
                        // Check if no neighbor has this color
                        if (neighborColorCount[bestV * K + c] == 0)
                        {
                            // Apply color
                            color[bestV] = c;
                            uncoloredCount--;

                            // We must now update data for all neighbors
                            foreach (int u in adj[bestV])
                            {
                                int idx = u * K + c;
                                if (neighborColorCount[idx] == 0) neighborColors[u]++;
                                neighborColorCount[idx]++;
                                if (color[u] == -1 && !temporarilyDeleted[u]) uncoloredNeighbors[u]--;
                            }

                            // Go down one level 
                            pathMaxColor[level + 1] = Math.Max(pathMaxColor[level], c);
                            state[level + 1] = 0;
                            level++;
                        }
                        else
                        {
                            // Color c was taken by a neighbor, take the next one 
                            pathC[level]++;
                        }
                    }
                    // If (c > pathColorLimit)
                    else
                    {
                        // This means this vertex doesn't fit any color with the current coloring,
                        // so we must backtrack.

                        // Undo all vertex postponements 
                        for (int i = 0; i < pathTempDeletedCount[level]; i++)
                        {
                            int v = temporarilyDeletedStack.Pop();
                            temporarilyDeleted[v] = false;
                            uncoloredCount++;
                            foreach (int u in adj[v])
                                if (color[u] == -1 && !temporarilyDeleted[u])
                                    uncoloredNeighbors[u]++;
                        }

                        // Backtrack to the higher level
                        level--;

                        if (level >= 0)
                        {
                            // Get vertex and color data
                            int prevV = pathV[level];
                            int prevC = pathC[level];

                            // Undo coloring of vertex prevV with color prevC
                            color[prevV] = -1;
                            uncoloredCount++;
                            foreach (int u in adj[prevV])
                            {
                                int idx = u * K + prevC;
                                neighborColorCount[idx]--;
                                if (neighborColorCount[idx] == 0) neighborColors[u]--;
                                if (color[u] == -1 && !temporarilyDeleted[u]) uncoloredNeighbors[u]++;
                            }

                            // Choose the next color for this vertex
                            pathC[level]++;
                        }
                    }
                }
            }
            return false;
        }
    }
}
