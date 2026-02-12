using System;
using System.Collections.Generic;

using Algorithms.Common.Types;

namespace Algorithms.LA;

/// <summary>
/// The Table Algorithm: O(n²) preprocessing, O(1) query time.
/// Stores answers to all possible level ancestor queries in a lookup table.
/// </summary>
public class LevelAncestorTable : ILAAlgorithm
{
    private const int MaxNodes = 1000;

    private readonly int _n;
    private readonly List<int>[] _children;
    private readonly int[] _depth;
    private readonly int[][] _table; // table[v][d] = ancestor of v at depth d

    public LevelAncestorTable(int[] parent) : this(parent.Length)
    {
        for (var i = 1; i < parent.Length; i++)
        {
            if (parent[i] >= 0)
            {
                AddEdge(parent[i], i);
            }
        }
        Preprocess(0);
    }

    public LevelAncestorTable(int n)
    {
        if (n > MaxNodes)
        {
            throw new ArgumentException(
                $"LevelAncestorTable has O(n²) space and time complexity. " +
                $"For n={n} nodes, this would require {(long)n * n} table entries. " +
                $"Maximum supported size is {MaxNodes} nodes. " +
                $"Consider using LevelAncestorJumpPointers (O(n log n)) or other algorithms for larger trees.",
                nameof(n));
        }

        if (n <= 0)
        {
            throw new ArgumentException("Number of nodes must be positive.", nameof(n));
        }

        _n = n;
        _children = new List<int>[n];
        _depth = new int[n];
        _table = new int[n][];

        for (var i = 0; i < n; i++)
        {
            _children[i] = [];
            _table[i] = new int[n];
            Array.Fill(_table[i], -1); // -1 means no ancestor at that depth
        }
    }

    public ComplexityEnum BuildComplexity => ComplexityEnum.Quadratic;

    public ComplexityEnum QueryComplexity => ComplexityEnum.Constant;

    public void AddEdge(int parent, int child)
    {
        _children[parent].Add(child);
    }

    /// <summary>
    /// Preprocess the tree rooted at 'root'. O(n²) time.
    /// Builds a complete lookup table for all level ancestor queries.
    /// </summary>
    public void Preprocess(int root)
    {
        // First, compute depths using BFS
        var queue = new Queue<int>();
        var visited = new bool[_n];
        _depth[root] = 0;
        visited[root] = true;
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var u = queue.Dequeue();
            foreach (var child in _children[u])
            {
                if (!visited[child])
                {
                    visited[child] = true;
                    _depth[child] = _depth[u] + 1;
                    queue.Enqueue(child);
                }
            }
        }

        // Fill the table: for each node v and depth d < depth(v)
        // LA(v, d) = LA(parent(v), d)
        _table[root][0] = root;
        FillTable(root);
    }

    private void FillTable(int node)
    {
        foreach (var child in _children[node])
        {
            // Copy ancestors from parent: _table[child][d] = _table[node][d]
            for (var d = 0; d <= _depth[node]; d++)
            {
                _table[child][d] = _table[node][d];
            }

            // Set the child itself at its own depth
            _table[child][_depth[child]] = child;

            FillTable(child);
        }
    }

    /// <summary>
    /// Returns the ancestor of u at depth d. O(1) time.
    /// </summary>
    public int Query(int u, int d)
    {
        if (d > _depth[u] || d < 0)
        {
            return -1; // doesn't exist
        }

        return _table[u][d];
    }

    // --- Demo ---
    public static void Test()
    {
        // Build the example tree: A(0)-B(1)-D(2)-E(3)-F(4), A(0)-C(5)
        var la = new LevelAncestorTable(6);
        // Nodes: A=0, B=1, D=2, E=3, F=4, C=5
        la.AddEdge(0, 1); // A -> B
        la.AddEdge(0, 5); // A -> C
        la.AddEdge(1, 2); // B -> D
        la.AddEdge(2, 3); // D -> E
        la.AddEdge(3, 4); // E -> F

        la.Preprocess(0);

        // LA(F, 1) => B (node 1)
        Console.WriteLine($"LA(F, 1) = node {la.Query(4, 1)}"); // 1 (B)
        // LA(F, 0) => A (node 0)
        Console.WriteLine($"LA(F, 0) = node {la.Query(4, 0)}"); // 0 (A)
        // LA(F, 3) => E (node 3)
        Console.WriteLine($"LA(F, 3) = node {la.Query(4, 3)}"); // 3 (E)
        // LA(C, 0) => A (node 0)
        Console.WriteLine($"LA(C, 0) = node {la.Query(5, 0)}"); // 0 (A)
        // LA(D, 2) => D (node 2)
        Console.WriteLine($"LA(D, 2) = node {la.Query(2, 2)}"); // 2 (D)
    }
}
