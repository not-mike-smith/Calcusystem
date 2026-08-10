using Calcusystem.Measurement;

namespace Calcusystem.DimensionedExpression.State;

/// <summary>Which two-argument expression a <see cref="BinaryExpressionState"/> rebuilds into.</summary>
public enum BinaryExpressionKind
{
    /// <summary>Numerator over denominator.</summary>
    Quotient,
}

/// <summary>
/// The complete stored state of an expression over exactly two ordered children.
/// </summary>
/// <remarks>
/// Named for the arity rather than for its single current occupant, so that Milestone 5's <c>PowerExpression</c>
/// joins by adding a <see cref="BinaryExpressionKind"/> member rather than by renaming this record.
/// </remarks>
/// <param name="Kind">Which expression this state rebuilds into.</param>
/// <param name="Id">Stable identity.</param>
/// <param name="InnerId1">Id of the first child (a quotient's numerator).</param>
/// <param name="InnerId2">Id of the second child (a quotient's denominator).</param>
/// <param name="ErrorPropagation">How child uncertainties are combined.</param>
public readonly record struct BinaryExpressionState(
    BinaryExpressionKind Kind,
    string Id,
    string InnerId1,
    string InnerId2,
    ErrorPropagationMethod ErrorPropagation);
