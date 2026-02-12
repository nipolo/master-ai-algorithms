using System;
using System.Collections.Generic;

using Algorithms.Common.Types;

namespace Algorithms.LA;

public class LevelAncestorLadder : ILAAlgorithm
{
    private readonly int _n;
    private readonly List<int>[] _children;
    private readonly int[] _parent;
    private readonly int[] _depth;
    private readonly int[] _height;

    // For each node, which ladder it belongs to and its index in that ladder
    private readonly int[] _ladderIndex;    // index of node within its ladder
    private readonly int[] _nodeLadderId;   // which ladder a node belongs to

    private readonly List<List<int>> _allLadders = [];

    public LevelAncestorLadder(int[] parent) : this(parent.Length)
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

    public LevelAncestorLadder(int n)
    {
        _n = n;
        _children = new List<int>[n];
        _parent = new int[n];
        _depth = new int[n];
        _height = new int[n];
        _ladderIndex = new int[n];
        _nodeLadderId = new int[n];

        for (var i = 0; i < n; i++)
        {
            _children[i] = [];
            _parent[i] = -1;
            _nodeLadderId[i] = -1;
        }
    }

    public ComplexityEnum BuildComplexity => ComplexityEnum.Linear;

    public ComplexityEnum QueryComplexity => ComplexityEnum.Logarithmic;

    /// <summary>
    /// Query: LA(u, d) in O(log n) time.
    /// Climb ladders until we reach the target depth.
    /// </summary>
    public int Query(int u, int d)
    {
        if (d > _depth[u])
        {
            return -1;
        }

        if (d == _depth[u])
        {
            return u;
        }

        while (true)
        {
            var ladderId = _nodeLadderId[u];
            var ladder = _allLadders[ladderId];
            var posInLadder = _ladderIndex[u];

            var topDepth = _depth[ladder[0]];

            if (d >= topDepth)
            {
                // Target is within this ladder
                var targetPos = posInLadder - (_depth[u] - d);

                return ladder[targetPos];
            }
            else
            {
                // Move to parent of top node in current ladder to switch to a different ladder
                u = _parent[ladder[0]];
                if (u == -1)
                {
                    return -1;
                }
            }
        }
    }

    private void AddEdge(int parent, int child)
    {
        _children[parent].Add(child);
        _parent[child] = parent;
    }

    /// <summary>
    /// Preprocess: O(n) time
    /// </summary>
    private void Preprocess(int root)
    {
        ComputeDepthAndHeight(root);
        LongPathDecomposition(root);
        ExtendLaddersWithDoubling();
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

    private void LongPathDecomposition(int root)
    {
        // For each node, pick the child with maximum height as the
        // "long path child". This greedily forms the long-path decomposition.
        var longPathChild = new int[_n];

        for (var v = 0; v < _n; v++)
        {
            var bestChild = -1;
            var bestHeight = 0;
            foreach (var child in _children[v])
            {
                if (_height[child] > bestHeight)
                {
                    bestHeight = _height[child];
                    bestChild = child;
                }
            }
            longPathChild[v] = bestChild;
        }

        // Extract long paths: start from nodes whose parent doesn't
        // continue the same long path (i.e., v is NOT the longPathChild of its parent)

        for (var v = 0; v < _n; v++)
        {
            // v is a path head if its parent's longPathChild is not v
            var isHead = (_parent[v] == -1) || (longPathChild[_parent[v]] != v);

            if (isHead)
            {
                // Walk down the long path from v
                var path = new List<int>();
                var cur = v;
                while (cur != -1)
                {
                    path.Add(cur);

                    var ladderId = _allLadders.Count;
                    _nodeLadderId[cur] = ladderId;
                    _ladderIndex[cur] = path.Count - 1;

                    cur = longPathChild[cur];
                }
                _allLadders.Add(path);
            }
        }
    }

    private void ExtendLaddersWithDoubling()
    {
        for (var i = 0; i < _allLadders.Count; i++)
        {
            var path = _allLadders[i];

            var extension = new List<int>();
            var cur = _parent[path[0]];

            for (var step = 0; step < path.Count && cur != -1; step++)
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

    // --- Demo ---
    public static void Test()
    {
        //          0:A
        //          |
        //          1:B
        //         / \
        //       2:C  6:G
        //       |
        //       3:D
        //      / \
        //    4:E  7:H
        //    |
        //    5:F

        var la = new LevelAncestorLadder(8);
        la.AddEdge(0, 1); // A->B
        la.AddEdge(1, 2); // B->C
        la.AddEdge(1, 6); // B->G
        la.AddEdge(2, 3); // C->D
        la.AddEdge(3, 4); // D->E
        la.AddEdge(3, 7); // D->H
        la.AddEdge(4, 5); // E->F

        la.Preprocess(0);

        Console.WriteLine($"LA(F, 0) = {la.Query(5, 0)}"); // 0 (A)
        Console.WriteLine($"LA(F, 1) = {la.Query(5, 1)}"); // 1 (B)
        Console.WriteLine($"LA(F, 3) = {la.Query(5, 3)}"); // 3 (D)
        Console.WriteLine($"LA(H, 0) = {la.Query(7, 0)}"); // 0 (A)
        Console.WriteLine($"LA(H, 1) = {la.Query(7, 1)}"); // 1 (B)
        Console.WriteLine($"LA(G, 0) = {la.Query(6, 0)}"); // 0 (A)
    }
}
