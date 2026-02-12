using System;
using System.Collections.Generic;

using Algorithms.Common.Types;

namespace Algorithms.LA;

public class LevelAncestorJumpPointers : ILAAlgorithm
{
    private readonly int _n;
    private readonly int _logN;
    private readonly List<int>[] _children;
    private readonly int[] _depth;
    private readonly int[][] _jump; // jump[v][i] = 2^i-th ancestor of v

    public LevelAncestorJumpPointers(int[] parent) : this(parent.Length)
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

    public LevelAncestorJumpPointers(int n)
    {
        _n = n;
        _logN = (int)Math.Ceiling(Math.Log2(n + 1)) + 1;
        _children = new List<int>[n];
        _depth = new int[n];
        _jump = new int[n][];

        for (var i = 0; i < n; i++)
        {
            _children[i] = [];
            _jump[i] = new int[_logN];
            Array.Fill(_jump[i], -1); // -1 means no ancestor
        }
    }

    public ComplexityEnum BuildComplexity => ComplexityEnum.Linearithmic;

    public ComplexityEnum QueryComplexity => ComplexityEnum.Linear;

    /// <summary>
    /// Returns the ancestor of u at depth d. O(log n)
    /// </summary>
    public int Query(int u, int d)
    {
        if (d > _depth[u])
        {
            return -1; // doesn't exist
        }

        if (d == _depth[u])
        {
            return u;
        }

        var stepsUp = _depth[u] - d; // δ in the paper

        // Greedily follow the largest jump pointer that doesn't overshoot
        for (var i = _logN - 1; i >= 0; i--)
        {
            if (stepsUp >= (1 << i))
            {
                u = _jump[u][i];
                stepsUp -= 1 << i;
            }
        }

        return u;
    }

    private void AddEdge(int parent, int child)
    {
        _children[parent].Add(child);
    }

    /// <summary>
    /// Preprocess the tree rooted at 'root'. O(n log n)
    /// </summary>
    private void Preprocess(int root)
    {
        // BFS to set depths and direct parent (jump[v][0])
        var queue = new Queue<int>();

        _depth[root] = 0;
        _jump[root][0] = -1; // root has no parent
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var u = queue.Dequeue();
            foreach (var child in _children[u])
            {
                _depth[child] = _depth[u] + 1;
                _jump[child][0] = u; // parent
                queue.Enqueue(child);
            }
        }

        // Fill jump table: jump[v][i] = jump[jump[v][i-1]][i-1]
        for (var i = 1; i < _logN; i++)
        {
            for (var v = 0; v < _n; v++)
            {
                var halfway = _jump[v][i - 1];
                _jump[v][i] = (halfway == -1) ? -1 : _jump[halfway][i - 1];
            }
        }
    }

    // --- Demo ---
    public static void Test()
    {
        // Build the example tree:  A(0)-B(1)-D(2)-E(3)-F(4), A(0)-C(5)
        var la = new LevelAncestorJumpPointers(6);
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
    }
}
