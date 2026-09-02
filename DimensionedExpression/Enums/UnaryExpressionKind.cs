
namespace Calcusystem.DimensionedExpression.Enums;

/// <summary>Which single-argument expression a <see cref="UnaryExpressionState"/> rebuilds into.</summary>
public enum UnaryExpressionKind
{
    /// <summary><c>1/x</c>.</summary>
    Reciprocal,

    /// <summary><c>-x</c>.</summary>
    Negated,

    /// <summary><c>√x</c>.</summary>
    Sqrt,

    /// <summary><c>e^x</c>.</summary>
    Exponential,

    /// <summary><c>ln x</c>.</summary>
    NaturalLog,
}
