using Algorithms.Common.Types;

namespace Algorithms.LA;

public interface ILAAlgorithm
{
    /// <summary>
    /// Preprocessing (build) time complexity
    /// </summary>
    ComplexityEnum BuildComplexity { get; }

    /// <summary>
    /// Query time complexity
    /// </summary>
    ComplexityEnum QueryComplexity { get; }

    int Query(int node, int targetDepth);
}
