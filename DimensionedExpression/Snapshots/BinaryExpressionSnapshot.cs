using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.Snapshots;

/// <summary>
/// The complete snapshot of an expression over exactly two ordered children.
/// </summary>
/// <remarks>
/// Named for the arity rather than for its single current occupant, so another two-child expression joins by
/// adding a <see cref="BinaryExpressionType"/> member rather than by renaming this record.
/// </remarks>
/// <param name="Type">Which expression this snapshot rebuilds into.</param>
/// <param name="Id">Stable identity.</param>
/// <param name="InnerId1">Id of the first child (a quotient's numerator).</param>
/// <param name="InnerId2">Id of the second child (a quotient's denominator).</param>
/// <param name="UncertaintyCorrelation">Whether the children's uncertainties are treated as correlated.</param>
public readonly record struct BinaryExpressionSnapshot(
    BinaryExpressionType Type,
    string Id,
    string InnerId1,
    string InnerId2,
    UncertaintyCorrelation UncertaintyCorrelation);
