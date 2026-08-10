namespace Calcusystem.DimensionedExpression.State;

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

/// <summary>
/// The complete stored state of any expression wrapping a single argument.
/// </summary>
/// <remarks>
/// Grouped by arity rather than by type, as the uncertainty and provenance states are: the five kinds differ in
/// what they compute, not in what has to be stored to rebuild them. The semantic difference lives in
/// <see cref="Kind"/>.
/// </remarks>
/// <param name="Kind">Which expression this state rebuilds into.</param>
/// <param name="Id">Stable identity.</param>
/// <param name="InnerId">Id of the argument expression.</param>
public readonly record struct UnaryExpressionState(UnaryExpressionKind Kind, string Id, string InnerId);
