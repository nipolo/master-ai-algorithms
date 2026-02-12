using System;
using System.Diagnostics;

using Algorithms.Common.Types;
using Algorithms.LA;

namespace Algorithms.Tests;

public class LevelAncestorTests
{
    private readonly Func<int[], ILAAlgorithm> _laFactory;

    public LevelAncestorTests()
    {
        //_laFactory = (parents) => new LevelAncestorTable(parents);
        //_laFactory = (parents) => new LevelAncestorLadder(parents);
        //_laFactory = (parents) => new LevelAncestorLadder(parents);
        //_laFactory = (parents) => new LevelAncestorJumpAndLadder(parents);
        _laFactory = (parents) => new LevelAncestorOptimal(parents);
    }

    [Fact]
    public void BasicTree_LA_ReturnsCorrectAncestors()
    {
        // Tree:     0
        //          / \
        //         1   2
        //        / \   \
        //       3   4   5
        //      /
        //     6
        int[] parents = [-1, 0, 0, 1, 1, 2, 3];
        var la = _laFactory(parents);

        Assert.Equal(0, la.Query(6, 0)); // Root at depth 0
        Assert.Equal(1, la.Query(6, 1)); // Ancestor at depth 1
        Assert.Equal(3, la.Query(6, 2)); // Ancestor at depth 2
        Assert.Equal(6, la.Query(6, 3)); // Self at depth 3
        Assert.Equal(-1, la.Query(6, 4)); // Invalid depth
    }

    [Fact]
    public void SingleNode_LA_Works()
    {
        int[] parents = [-1];
        var la = _laFactory(parents);

        Assert.Equal(0, la.Query(0, 0));
        Assert.Equal(-1, la.Query(0, 1));
    }

    [Fact]
    public void LinearTree_LA_Works()
    {
        // 0 -> 1 -> 2 -> 3 -> 4 -> 5
        int[] parents = [-1, 0, 1, 2, 3, 4];
        var la = _laFactory(parents);

        Assert.Equal(0, la.Query(5, 0));
        Assert.Equal(1, la.Query(5, 1));
        Assert.Equal(2, la.Query(5, 2));
        Assert.Equal(3, la.Query(5, 3));
        Assert.Equal(4, la.Query(5, 4));
        Assert.Equal(5, la.Query(5, 5));
    }

    [Fact]
    public void BinaryTree_LA_Works()
    {
        //       0
        //      / \
        //     1   2
        //    / \ / \
        //   3  4 5  6
        int[] parents = [-1, 0, 0, 1, 1, 2, 2];
        var la = _laFactory(parents);

        Assert.Equal(0, la.Query(6, 0));
        Assert.Equal(2, la.Query(6, 1));
        Assert.Equal(6, la.Query(6, 2));

        Assert.Equal(0, la.Query(3, 0));
        Assert.Equal(1, la.Query(3, 1));
        Assert.Equal(3, la.Query(3, 2));
    }

    [Fact]
    public void InvalidDepth_ReturnsMinusOne()
    {
        // 0 -> 1 -> 2 -> 3
        int[] parents = [-1, 0, 1, 2];
        var la = _laFactory(parents);

        Assert.Equal(-1, la.Query(3, 10)); // Too deep
        Assert.Equal(-1, la.Query(3, -1)); // Negative
    }

    [Fact]
    public void LargeLinearTree_VerifiesComplexity()
    {
        if (_laFactory([-1]).BuildComplexity == ComplexityEnum.Quadratic)
        {
            return;
        }

        // Test O(n) preprocessing
        var n = 10000;
        var parents = new int[n];
        parents[0] = -1;
        for (var i = 1; i < n; i++)
        {
            parents[i] = i - 1;
        }

        var sw = Stopwatch.StartNew();
        var la = _laFactory(parents);
        sw.Stop();

        // Preprocessing should be fast (wall-clock timing is environment-dependent)
        Assert.True(sw.ElapsedMilliseconds < 5000);

        // Verify correctness
        Assert.Equal(0, la.Query(9999, 0));
        Assert.Equal(5000, la.Query(9999, 5000));
        Assert.Equal(9999, la.Query(9999, 9999));
    }

    [Fact]
    public void LargeLinearTree_VerifiesO1Query()
    {
        if (_laFactory([-1]).BuildComplexity == ComplexityEnum.Quadratic)
        {
            return;
        }

        // Test O(1) query time
        var n = 10000;
        var parents = new int[n];
        parents[0] = -1;
        for (var i = 1; i < n; i++)
        {
            parents[i] = i - 1;
        }
        var la = _laFactory(parents);

        var sw = Stopwatch.StartNew();
        var iterations = 10000;
        for (var i = 0; i < iterations; i++)
        {
            var target = i * 17 % n;
            _ = la.Query(n - 1, target);
        }
        sw.Stop();

        // Average query should be very fast (wall-clock timing is environment-dependent)
        var avgTime = (double)sw.ElapsedMilliseconds / iterations;
        Assert.True(avgTime < 0.5);
    }

    [Fact]
    public void CompleteBinaryTree_LA_Works()
    {
        // Binary tree with 127 nodes (height 6)
        var n = 127;
        var parents = new int[n];
        parents[0] = -1;
        for (var i = 1; i < n; i++)
        {
            parents[i] = (i - 1) / 2;
        }
        var la = _laFactory(parents);

        Assert.Equal(0, la.Query(126, 0));
        Assert.Equal(62, la.Query(126, 5));
        Assert.Equal(126, la.Query(126, 6));
    }

    [Fact]
    public void WideTree_LA_Works()
    {
        // Root with 9 children
        int[] parents = [-1, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var la = _laFactory(parents);

        for (var i = 1; i < 10; i++)
        {
            Assert.Equal(0, la.Query(i, 0));
            Assert.Equal(i, la.Query(i, 1));
        }
    }

    [Fact]
    public void SkewedTree_LA_Works()
    {
        // Right-skewed: 0 -> 1 -> 2 -> 3
        int[] parents = [-1, 0, 1, 2];
        var la = _laFactory(parents);

        Assert.Equal(0, la.Query(3, 0));
        Assert.Equal(1, la.Query(3, 1));
        Assert.Equal(2, la.Query(3, 2));
        Assert.Equal(3, la.Query(3, 3));
    }

    [Fact]
    public void StressTest_RandomQueries()
    {
        var n = 1000;
        var parents = new int[n];
        parents[0] = -1;

        var rnd = new Random(42);
        for (var i = 1; i < n; i++)
        {
            parents[i] = rnd.Next(0, i);
        }

        var la = _laFactory(parents);

        // Perform random LA queries
        for (var i = 0; i < 1000; i++)
        {
            var node = rnd.Next(0, n);
            var nodeDepth = GetDepth(parents, node);

            if (nodeDepth > 0)
            {
                var targetDepth = rnd.Next(0, nodeDepth + 1);
                var ancestor = la.Query(node, targetDepth);

                Assert.True(ancestor >= 0 && ancestor < n);
                Assert.Equal(targetDepth, GetDepth(parents, ancestor));
            }
        }
    }

    [Fact]
    public void LargeTree_100k_Nodes()
    {
        if (_laFactory([-1]).BuildComplexity == ComplexityEnum.Quadratic)
        {
            return;
        }

        // Test with 100k nodes
        var n = 100000;
        var parents = new int[n];
        parents[0] = -1;
        for (var i = 1; i < n; i++)
        {
            parents[i] = i - 1;
        }

        var sw = Stopwatch.StartNew();
        var la = _laFactory(parents);
        sw.Stop();

        // Should still be O(n)
        Assert.True(sw.ElapsedMilliseconds < 5000);

        // Test query
        Assert.Equal(0, la.Query(99999, 0));
        Assert.Equal(50000, la.Query(99999, 50000));
    }

    // Helper to calculate depth (for verification)
    private static int GetDepth(int[] parents, int node)
    {
        var depth = 0;
        while (parents[node] != -1)
        {
            depth++;
            node = parents[node];
        }
        return depth;
    }
}


