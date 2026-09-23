using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.State;

/// <summary>
/// The complete stored state of an expression combining any number of children.
/// </summary>
/// <param name="Kind">Which expression this state rebuilds into.</param>
/// <param name="Id">Stable identity.</param>
/// <param name="InnerIds">Ids of the children, in order.</param>
/// <param name="ErrorPropagation">Whether the children's errors are treated as correlated. Renamed to <c>ErrorCorrelation</c> in a pending TODO — see <c>ErrorPropagationMethod</c>.</param>
public readonly record struct NaryExpressionState(
    NaryExpressionKind Kind,
    string Id,
    IReadOnlyList<string> InnerIds,
    ErrorPropagationMethod ErrorPropagation);
