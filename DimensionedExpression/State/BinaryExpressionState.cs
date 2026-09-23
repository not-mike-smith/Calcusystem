using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.State;

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
/// <param name="ErrorPropagation">Whether the children's errors are treated as correlated. Renamed to <c>ErrorCorrelation</c> in a pending TODO — see <c>ErrorPropagationMethod</c>.</param>
public readonly record struct BinaryExpressionState(
    BinaryExpressionKind Kind,
    string Id,
    string InnerId1,
    string InnerId2,
    ErrorPropagationMethod ErrorPropagation);
