namespace Algorithms.Common.Types;

/// <summary>
/// Represents algorithmic complexity classes
/// </summary>
public enum ComplexityEnum
{
    /// <summary>O(1) - Constant time</summary>
    Constant = 1,

    /// <summary>O(log n) - Logarithmic time</summary>
    Logarithmic = 2,

    /// <summary>O(n) - Linear time</summary>
    Linear = 3,

    /// <summary>O(n log n) - Linearithmic time</summary>
    Linearithmic = 4,

    /// <summary>O(n²) - Quadratic time</summary>
    Quadratic = 5,

    /// <summary>O(√n) - Square root time</summary>
    SquareRoot = 6
}
