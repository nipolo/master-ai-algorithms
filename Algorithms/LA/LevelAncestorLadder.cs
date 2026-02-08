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

    public void AddEdge(int parent, int child)
    {
        _children[parent].Add(child);
        _parent[child] = parent;
    }

    /// <summary>
    /// Preprocess: O(n) time
    /// </summary>
    public void Preprocess(int root)
    {
        ComputeDepthAndHeight(root);
        LongPathDecomposition(root);
        ExtendLaddersWithDoubling();
    }

    private void ComputeDepthAndHeight(int root)
    {
        // Iterative post-order to compute depth and height
        var stack = new Stack<(int node, bool processed)>();
        _depth[root] = 0;
        stack.Push((root, false));

        while (stack.Count > 0)
        {
            var (node, processed) = stack.Pop();

            if (processed)
            {
                // Compute height from children
                _height[node] = 1; // leaves have height 1
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
        Array.Fill(longPathChild, -1);

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
        var onLongPath = new bool[_n];

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
                    onLongPath[cur] = true;

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
        // For each ladder (long path), extend UPWARD by prepending ancestors.
        // A path of length h gets extended by h nodes above its top.
        for (var i = 0; i < _allLadders.Count; i++)
        {
            var path = _allLadders[i];
            var h = path.Count; // length of original long path

            // Prepend up to h ancestors above the top of the path
            var extension = new List<int>();
            var cur = _parent[path[0]]; // parent of the topmost node

            for (var step = 0; step < h && cur != -1; step++)
            {
                extension.Add(cur);
                cur = _parent[cur];
            }

            // Build the new ladder: extension (reversed) + original path
            extension.Reverse();
            var ladder = new List<int>(extension);
            ladder.AddRange(path);

            // Update indices: the original nodes shift by extension.Count
            var offset = extension.Count;
            foreach (var node in path)
            {
                _ladderIndex[node] = offset + _ladderIndex[node];
                // _nodeLadderId stays the same
            }

            // Note: extended nodes keep their OWN ladder assignment
            // (they already belong to a different ladder).
            // We don't reassign them — they just appear in this ladder too.

            _allLadders[i] = ladder;
        }
    }

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

            // How far up can we go in this ladder?
            // The node at ladder position 0 has the shallowest depth.
            // Depth of node at position p: depth[ladder[p]]
            var topDepth = _depth[ladder[0]];

            if (d >= topDepth)
            {
                // Target is within this ladder!
                // The target is at position: posInLadder - (depth[u] - d)
                var targetPos = posInLadder - (_depth[u] - d);
                return ladder[targetPos];
            }
            else
            {
                // Jump to top of this ladder, then step to parent
                var topNode = ladder[0];
                u = topNode;

                // If we're already at or above target, step carefully
                if (_depth[u] <= d)
                {
                    return Query(u, d); // re-enter with new u
                }

                // Move to parent to switch to a different ladder
                u = _parent[u];
                if (u == -1)
                {
                    return -1;
                }

                // Optimization: if we've reached target depth, return
                if (_depth[u] == d)
                {
                    return u;
                }
            }
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
