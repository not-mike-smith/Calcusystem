using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.Snapshots;

/// <summary>
/// The complete stored state of an expression combining any number of children.
/// </summary>
/// <param name="Type">Which expression this state rebuilds into.</param>
/// <param name="Id">Stable identity.</param>
/// <param name="InnerIds">Ids of the children, in order.</param>
/// <param name="UncertaintyPropagation">Whether the children's errors are treated as correlated. Renamed to <c>ErrorCorrelation</c> in a pending TODO — see <c>UncertaintyPropagation</c>.</param>
public readonly record struct NaryExpressionSnapshot(
    NaryExpressionType Type,
    string Id,
    IReadOnlyList<string> InnerIds,
    UncertaintyPropagation UncertaintyPropagation);
