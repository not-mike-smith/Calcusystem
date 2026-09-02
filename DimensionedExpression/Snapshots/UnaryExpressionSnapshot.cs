using Calcusystem.DimensionedExpression.Enums;

namespace Calcusystem.DimensionedExpression.Snapshots;

/// <summary>
/// The complete stored state of any expression wrapping a single argument.
/// </summary>
/// <remarks>
/// Grouped by arity rather than by type, as the uncertainty and provenance states are: the five kinds differ in
/// what they compute, not in what has to be stored to rebuild them. The semantic difference lives in
/// <see cref="Type"/>.
/// </remarks>
/// <param name="Type">Which expression this state rebuilds into.</param>
/// <param name="Id">Stable identity.</param>
/// <param name="InnerId">Id of the argument expression.</param>
public readonly record struct UnaryExpressionSnapshot(UnaryExpressionType Type, string Id, string InnerId);
