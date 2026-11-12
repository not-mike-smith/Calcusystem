
namespace Calcusystem.DimensionedExpression.Snapshots;

/// <summary>
/// The complete snapshot of an <see cref="Systems.ExpressionSystem"/>: its identity, its labels, and the ids
/// of everything it contains.
/// </summary>
/// <remarks>
/// The one node whose references are not all the same type: expressions in most of its lists, operators in the
/// last. Resolution is therefore a per-reference query rather than one typed delegate.
/// </remarks>
/// <param name="Id">Stable identity.</param>
/// <param name="Name">Human-readable name.</param>
/// <param name="Description">Human-readable description.</param>
/// <param name="VariableIds">Ids of the variables, including any reached through the other lists.</param>
/// <param name="GivenIds">Ids of the constants and reference values.</param>
/// <param name="ScalarIds">Ids of the scalars.</param>
/// <param name="DerivedExpressionIds">Ids of the computed expressions, including nodes nested inside others.</param>
/// <param name="RelationshipIds">
/// Ids of every asserted relationship, definitions and constraints alike. One list, because which one a
/// relationship is rides on the operator's own <c>SolvingRole</c> rather than on where it is filed.
/// </param>
public readonly record struct ExpressionSystemSnapshot(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string> VariableIds,
    IReadOnlyList<string> GivenIds,
    IReadOnlyList<string> ScalarIds,
    IReadOnlyList<string> DerivedExpressionIds,
    IReadOnlyList<string> RelationshipIds);
