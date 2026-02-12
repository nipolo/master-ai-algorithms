using System;
using System.Collections.Generic;

using Algorithms.Common.Types;

namespace Algorithms.LA;

/// <summary>
/// Minimal O(n) preprocessing, O(1) query Level Ancestor.
/// Bender & Farach-Colton's Macro-Micro-Tree Algorithm.
/// </summary>
public class LevelAncestorOptimal : ILAAlgorithm
{
    private readonly int _nodeCount;
    private readonly int _logNodeCount;
    private readonly int _microTreeMaxSize;

    // Tree structure
    private readonly List<int>[] _children;
    private readonly int[] _parent;
    private readonly int[] _depth;
    private readonly int[] _height;
    private readonly int[] _subtreeSize;

    // Ladders
    private readonly int[] _nodeLadderId;
    private readonly int[] _nodeLadderPosition;
    private readonly List<int[]> _ladders = [];

    // Jump pointers (only on jump nodes)
    private readonly int[][] _jumpPointers;
    private readonly bool[] _isJumpNode;
    private readonly int[] _jumpDescendant;

    // Microtree data
    private readonly bool[] _isMicroNode;
    private readonly int[] _microTreeRoot;
    private readonly int[] _microDfsIndex;
    private readonly int[] _microLocalDepth;
    private readonly long[] _microTreeEncoding;
    private readonly Dictionary<long, int[,]> _microTables = [];
    private readonly Dictionary<int, int[]> _microDfsToNodeMap = [];
    private readonly Dictionary<int, int> _microTreeIdByRoot = [];

    public LevelAncestorOptimal(int[] parent) : this(parent.Length)
    {
        for (var i = 1; i < parent.Length; i++)
        {
            if (parent[i] >= 0)
            {
                AddEdge(parent[i], i);
            }
        }

        Build(0);
    }

    public LevelAncestorOptimal(int nodeCount)
    {
        _nodeCount = nodeCount;
        _logNodeCount = Math.Max(1, (int)Math.Ceiling(Math.Log2(nodeCount + 1)));
        _microTreeMaxSize = Math.Max(1, _logNodeCount / 4);

        _children = new List<int>[nodeCount];
        _parent = new int[nodeCount];
        _depth = new int[nodeCount];
        _height = new int[nodeCount];
        _subtreeSize = new int[nodeCount];
        _nodeLadderId = new int[nodeCount];
        _nodeLadderPosition = new int[nodeCount];
        _jumpPointers = new int[nodeCount][];
        _isJumpNode = new bool[nodeCount];
        _jumpDescendant = new int[nodeCount];
        _isMicroNode = new bool[nodeCount];
        _microTreeRoot = new int[nodeCount];
        _microDfsIndex = new int[nodeCount];
        _microLocalDepth = new int[nodeCount];
        _microTreeEncoding = new long[nodeCount];

        for (var i = 0; i < nodeCount; i++)
        {
            _children[i] = [];
            _parent[i] = -1;
            _jumpDescendant[i] = -1;
            _microTreeRoot[i] = -1;
        }
    }

    public ComplexityEnum BuildComplexity => ComplexityEnum.Linear;

    public ComplexityEnum QueryComplexity => ComplexityEnum.Constant;

    /// <summary> LA(node, targetDepth) in O(1) </summary>
    public int Query(int node, int targetDepth)
    {
        if (targetDepth < 0 || targetDepth > _depth[node])
        {
            return -1;
        }

        if (targetDepth == _depth[node])
        {
            return node;
        }

        // Case 1: node is in a microtree
        if (_isMicroNode[node])
        {
            var root = _microTreeRoot[node];
            var rootDepth = _depth[root];

            if (targetDepth >= rootDepth)
            {
                // Answer is within the microtree — table lookup
                var localTargetDepth = targetDepth - rootDepth;
                var encoding = _microTreeEncoding[node];
                var answerLocalIndexInMicroTree = _microTables[encoding][_microDfsIndex[node], localTargetDepth];
                var microTreeId = _microTreeIdByRoot[root];

                return _microDfsToNodeMap[microTreeId][answerLocalIndexInMicroTree];
            }

            // Answer is outside — jump to microtree root's parent (a macro node)
            node = _parent[root];
            if (node == -1)
            {
                return -1;
            }
        }

        // Case 2: macro node — one jump pointer + one ladder lookup
        if (_depth[node] == targetDepth)
        {
            return node;
        }

        var jumpNode = _jumpDescendant[node];
        var delta = _depth[jumpNode] - targetDepth;
        var jumpBits = (int)Math.Floor(Math.Log2(delta));
        var landed = _jumpPointers[jumpNode][jumpBits];

        if (_depth[landed] == targetDepth)
        {
            return landed;
        }

        var ladder = _ladders[_nodeLadderId[landed]];
        var position = _nodeLadderPosition[landed];
        var remainingDistance = _depth[landed] - targetDepth;

        return ladder[position - remainingDistance];
    }

    private void AddEdge(int parent, int child)
    {
        _children[parent].Add(child);
        _parent[child] = parent;
    }

    private void Build(int root)
    {
        ComputeDepthHeightSize(root);
        ClassifyMicroMacro(root);
        BuildLadders();
        IdentifyJumpNodes();
        BuildJumpPointers();
        FindJumpDescendants(root);
        PreprocessMicroTrees();
    }

    // Preprocessing steps

    private void ComputeDepthHeightSize(int root)
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
                _subtreeSize[node] = 1;
                foreach (var child in _children[node])
                {
                    _height[node] = Math.Max(_height[node], _height[child] + 1);
                    _subtreeSize[node] += _subtreeSize[child];
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

    private void ClassifyMicroMacro(int root)
    {
        for (var node = 0; node < _nodeCount; node++)
        {
            _isMicroNode[node] = _subtreeSize[node] <= _microTreeMaxSize;
        }

        var queue = new Queue<int>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            foreach (var child in _children[node])
            {
                if (_isMicroNode[child])
                {
                    _microTreeRoot[child] = _isMicroNode[node] ? _microTreeRoot[node] : child;
                }

                queue.Enqueue(child);
            }
        }
    }

    private void BuildLadders()
    {
        // Pick tallest child as long-path continuation
        var longPathChild = new int[_nodeCount];

        for (var v = 0; v < _nodeCount; v++)
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

        // Extract long paths
        for (var node = 0; node < _nodeCount; node++)
        {
            if (_parent[node] != -1 && longPathChild[_parent[node]] == node)
            {
                continue; // not a path head
            }

            var path = new List<int>();
            for (var current = node; current != -1; current = longPathChild[current])
            {
                _nodeLadderId[current] = _ladders.Count;
                _nodeLadderPosition[current] = path.Count;
                path.Add(current);
            }

            // Extend upward by path length (doubling)
            var extension = new List<int>();
            var ancestor = _parent[path[0]];
            for (var steps = path.Count; steps > 0 && ancestor != -1; steps--, ancestor = _parent[ancestor])
            {
                extension.Add(ancestor);
            }

            extension.Reverse();

            foreach (var pathNode in path)
            {
                _nodeLadderPosition[pathNode] += extension.Count;
            }

            extension.AddRange(path);
            _ladders.Add([.. extension]); // The same as add at index _ladders.Count
        }
    }

    private void IdentifyJumpNodes()
    {
        for (var node = 0; node < _nodeCount; node++)
        {
            if (_isMicroNode[node])
            {
                continue;
            }

            // If is macrotree leaf
            if (_children[node].TrueForAll(child => _isMicroNode[child]))
            {
                _isJumpNode[node] = true;
            }
        }
    }

    private void BuildJumpPointers()
    {
        for (var node = 0; node < _nodeCount; node++)
        {
            if (!_isJumpNode[node])
            {
                continue;
            }

            _jumpPointers[node] = new int[_logNodeCount];
            Array.Fill(_jumpPointers[node], -1);
            _jumpPointers[node][0] = _parent[node];

            for (var power = 1; power < _logNodeCount; power++)
            {
                var previous = _jumpPointers[node][power - 1];
                if (previous == -1)
                {
                    break;
                }

                _jumpPointers[node][power] = ClimbLadders(previous, 1 << (power - 1));
            }
        }
    }

    private void FindJumpDescendants(int root)
    {
        for (var node = 0; node < _nodeCount; node++)
        {
            if (_isJumpNode[node])
            {
                _jumpDescendant[node] = node;
            }
        }

        var stack = new Stack<(int node, bool processed)>();
        stack.Push((root, false));

        while (stack.Count > 0)
        {
            var (node, processed) = stack.Pop();
            if (processed)
            {
                if (_jumpDescendant[node] != -1 || _isMicroNode[node])
                {
                    continue;
                }

                foreach (var child in _children[node])
                {
                    if (_jumpDescendant[child] != -1)
                    {
                        _jumpDescendant[node] = _jumpDescendant[child];
                        break;
                    }
                }
            }
            else
            {
                stack.Push((node, true));
                foreach (var child in _children[node])
                {
                    stack.Push((child, false));
                }
            }
        }
    }

    private void PreprocessMicroTrees()
    {
        var microRootCounter = 0;

        for (var node = 0; node < _nodeCount; node++)
        {
            if (!_isMicroNode[node] || _microTreeRoot[node] != node)
            {
                continue; // We are interested only in micro tree roots
            }

            var treeRoot = node;
            _microTreeIdByRoot[treeRoot] = microRootCounter;

            // DFS to encode tree shape and assign local indices
            var microTreeNodes = new List<int>();
            long encoding = 0;
            var bitPosition = 0;

            var dfsStack = new Stack<(int node, bool processed)>();
            dfsStack.Push((treeRoot, false));

            while (dfsStack.Count > 0)
            {
                var (current, processed) = dfsStack.Pop();
                if (processed)
                {
                    if (current != treeRoot)
                    {
                        encoding |= 1L << bitPosition;
                        bitPosition++;
                    }
                }
                else
                {
                    if (current != treeRoot)
                    {
                        bitPosition++; // down edge, bit = 0
                    }

                    _microDfsIndex[current] = microTreeNodes.Count;
                    _microLocalDepth[current] = _depth[current] - _depth[treeRoot];
                    microTreeNodes.Add(current);

                    dfsStack.Push((current, true));
                    for (var i = _children[current].Count - 1; i >= 0; i--)
                    {
                        var child = _children[current][i];
                        if (_isMicroNode[child] && _microTreeRoot[child] == treeRoot)
                        {
                            dfsStack.Push((child, false));
                        }
                    }
                }
            }

            foreach (var treeNode in microTreeNodes)
            {
                _microTreeEncoding[treeNode] = encoding;
            }

            _microDfsToNodeMap[microRootCounter] = [.. microTreeNodes];

            if (!_microTables.ContainsKey(encoding))
            {
                BuildMicroTable(encoding, microTreeNodes);
            }

            microRootCounter++;
        }
    }

    // Helpers

    private void BuildMicroTable(long encoding, List<int> nodesInOrder)
    {
        var count = nodesInOrder.Count;
        var localParent = new int[count];
        var localDepth = new int[count];
        localParent[0] = -1;

        for (var i = 0; i < count; i++)
        {
            localDepth[i] = _depth[nodesInOrder[i]] - _depth[nodesInOrder[0]];
            if (i > 0)
            {
                for (var j = 0; j < i; j++)
                {
                    if (nodesInOrder[j] == _parent[nodesInOrder[i]])
                    {
                        localParent[i] = j;

                        break;
                    }
                }
            }
        }

        var table = new int[count, count];
        for (var i = 0; i < count; i++)
        {
            for (var j = 0; j < count; j++)
            {
                table[i, j] = -1;
            }
        }

        for (var i = 0; i < count; i++)
        {
            for (var current = i; current != -1; current = localParent[current])
            {
                table[i, localDepth[current]] = current;
            }
        }

        _microTables[encoding] = table;
    }

    private int ClimbLadders(int node, int steps)
    {
        var targetDepth = _depth[node] - steps;
        if (targetDepth < 0)
        {
            return -1;
        }

        while (_depth[node] > targetDepth)
        {
            var ladder = _ladders[_nodeLadderId[node]];
            var topDepth = _depth[ladder[0]];

            if (targetDepth >= topDepth)
            {
                return ladder[_nodeLadderPosition[node] - (_depth[node] - targetDepth)];
            }

            node = _parent[ladder[0]];
            if (node == -1)
            {
                return -1;
            }
        }

        return node;
    }

    // Test

    public static void Test()
    {
        var random = new Random(42);

        foreach (var nodeCount in new[] { 1, 2, 5, 15, 100, 500, 2000 })
        {
            var levelAncestor = new LevelAncestorOptimal(nodeCount);
            for (var i = 1; i < nodeCount; i++)
            {
                levelAncestor.AddEdge(random.Next(0, i), i);
            }

            levelAncestor.Build(0);

            int failures = 0, queries = 0;
            for (var node = 0; node < nodeCount; node++)
            {
                var expected = node;
                for (var depth = levelAncestor._depth[node]; depth >= 0; depth--)
                {
                    if (levelAncestor.Query(node, depth) != expected)
                    {
                        failures++;
                    }

                    expected = levelAncestor._parent[expected];
                    queries++;
                }
            }
            Console.WriteLine($"n={nodeCount,5}: {queries,7} queries, {failures} failures");
        }
    }
}
