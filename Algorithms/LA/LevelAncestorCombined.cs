using System;
using System.Collections.Generic;

using Algorithms.Common.Types;

namespace Algorithms.LA;

public class LevelAncestorCombined : ILAAlgorithm
{
    private readonly int _n;
    private readonly int _logN;
    private readonly List<int>[] _children;
    private readonly int[] _parent;
    private readonly int[] _depth;
    private readonly int[] _height;

    // Jump pointers
    private readonly int[][] _jump;

    // Ladders
    private readonly int[] _nodeLadderId;
    private readonly int[] _ladderIndex;
    private readonly List<List<int>> _allLadders = [];
    private readonly int[] _longPathChild;

    public LevelAncestorCombined(int[] parent) : this(parent.Length)
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

    public LevelAncestorCombined(int n)
    {
        _n = n;
        _logN = (int)Math.Ceiling(Math.Log2(n + 1)) + 1;
        _children = new List<int>[n];
        _parent = new int[n];
        _depth = new int[n];
        _height = new int[n];
        _jump = new int[n][];
        _nodeLadderId = new int[n];
        _ladderIndex = new int[n];
        _longPathChild = new int[n];

        for (var i = 0; i < n; i++)
        {
            _children[i] = [];
            _parent[i] = -1;
            _jump[i] = new int[_logN];
            Array.Fill(_jump[i], -1);
        }
    }

    public ComplexityEnum BuildComplexity => ComplexityEnum.Linearithmic;

    public ComplexityEnum QueryComplexity => ComplexityEnum.Constant;

    public void AddEdge(int parent, int child)
    {
        _children[parent].Add(child);
        _parent[child] = parent;
    }

    /// <summary>
    /// Full preprocessing: O(n log n) total
    /// - O(n) for depths, heights, ladders
    /// - O(n log n) for jump pointers
    /// </summary>
    public void Preprocess(int root)
    {
        ComputeDepthAndHeight(root);
        BuildJumpPointers(root);
        LongPathDecomposition();
        ExtendLadders();
    }

    private void ComputeDepthAndHeight(int root)
    {
        var stack = new Stack<(int node, bool processed)>();
        _depth[root] = 0;
        stack.Push((root, false));

        while (stack.Count > 0)
        {
            var (node, processed) = stack.Pop();
            if (processed)
            {
                _height[node] = 1;
                foreach (var child in _children[node])
                {
                    _height[node] = Math.Max(_height[node], _height[child] + 1);
                }
            }
            else
            {
                stack.Push((node, true));
                foreach (var child in _children[node])
                {
                    _depth[child] = _depth[node] + 1;
                    stack.Push((child, false));
                }
            }
        }
    }

    private void BuildJumpPointers(int root)
    {
        // BFS order to fill jump[v][0] = parent
        var queue = new Queue<int>();
        queue.Enqueue(root);
        _jump[root][0] = -1;

        while (queue.Count > 0)
        {
            var u = queue.Dequeue();
            foreach (var child in _children[u])
            {
                _jump[child][0] = u;
                queue.Enqueue(child);
            }
        }

        // Fill higher jumps: jump[v][i] = jump[jump[v][i-1]][i-1]
        for (var i = 1; i < _logN; i++)
        {
            for (var v = 0; v < _n; v++)
            {
                var mid = _jump[v][i - 1];
                _jump[v][i] = (mid == -1) ? -1 : _jump[mid][i - 1];
            }
        }
    }

    private void LongPathDecomposition()
    {
        Array.Fill(_longPathChild, -1);

        for (var v = 0; v < _n; v++)
        {
            foreach (var child in _children[v])
            {
                if (_longPathChild[v] == -1 || _height[child] > _height[_longPathChild[v]])
                {
                    _longPathChild[v] = child;
                }
            }
        }

        for (var v = 0; v < _n; v++)
        {
            if (_parent[v] != -1 && _longPathChild[_parent[v]] == v)
            {
                continue;
            }

            var path = new List<int>();
            var cur = v;
            while (cur != -1)
            {
                _nodeLadderId[cur] = _allLadders.Count;
                _ladderIndex[cur] = path.Count;
                path.Add(cur);
                cur = _longPathChild[cur];
            }
            _allLadders.Add(path);
        }
    }

    private void ExtendLadders()
    {
        for (var i = 0; i < _allLadders.Count; i++)
        {
            var path = _allLadders[i];
            var h = path.Count;

            var extension = new List<int>();
            var cur = _parent[path[0]];
            for (var step = 0; step < h && cur != -1; step++)
            {
                extension.Add(cur);
                cur = _parent[cur];
            }

            extension.Reverse();
            var offset = extension.Count;

            // Update indices for original path nodes
            foreach (var node in path)
            {
                _ladderIndex[node] += offset;
            }

            extension.AddRange(path);
            _allLadders[i] = extension;
        }
    }

    /// <summary>
    /// Query: O(1) — one jump pointer + one ladder lookup
    /// </summary>
    public int Query(int u, int d)
    {
        if (d < 0 || d > _depth[u])
        {
            return -1;
        }

        if (d == _depth[u])
        {
            return u;
        }

        var delta = _depth[u] - d;

        // Step 1: Follow one jump pointer to get at least halfway
        var jumpBits = (int)Math.Floor(Math.Log2(delta));
        var v = _jump[u][jumpBits];

        // Step 2: Look up the answer in v's ladder
        var ladderId = _nodeLadderId[v];
        var ladder = _allLadders[ladderId];
        var posInLadder = _ladderIndex[v];
        var remainingUp = _depth[v] - d;
        var targetPos = posInLadder - remainingUp;

        return ladder[targetPos];
    }

    // --- Demo ---
    public static void Test()
    {
        //    0:A - 1:B - 2:C - 3:D - 4:E - 5:F - 6:G - 7:H
        //                  \
        //                   8:X
        var la = new LevelAncestorCombined(9);
        la.AddEdge(0, 1);
        la.AddEdge(1, 2);
        la.AddEdge(2, 3);
        la.AddEdge(3, 4);
        la.AddEdge(4, 5);
        la.AddEdge(5, 6);
        la.AddEdge(6, 7);
        la.AddEdge(2, 8); // branch

        la.Preprocess(0);

        Console.WriteLine($"LA(H, 1) = {la.Query(7, 1)}"); // 1 (B)
        Console.WriteLine($"LA(H, 0) = {la.Query(7, 0)}"); // 0 (A)
        Console.WriteLine($"LA(H, 5) = {la.Query(7, 5)}"); // 5 (F)
        Console.WriteLine($"LA(X, 0) = {la.Query(8, 0)}"); // 0 (A)
        Console.WriteLine($"LA(X, 1) = {la.Query(8, 1)}"); // 1 (B)

        // Verify all queries
        Console.WriteLine("\n--- Exhaustive check ---");
        for (var u = 0; u < 9; u++)
        {
            for (var d = 0; d <= la._depth[u]; d++)
            {
                Console.WriteLine($"LA({u}, {d}) = {la.Query(u, d)}");
            }
        }
    }
}
