
namespace Calcusystem.DimensionedExpression.Snapshots;

/// <summary>
/// The complete snapshot of an <see cref="Systems.ExpressionSystem"/>: its identity, its labels, and the ids
/// of everything it contains.
/// </summary>
/// <remarks>
/// The one node whose references are not all the same type: expressions in two of its lists, operators in the
/// third. Resolution is therefore a per-reference query rather than one typed delegate.
/// </remarks>
/// <param name="Id">Stable identity.</param>
/// <param name="Name">Human-readable name.</param>
/// <param name="Description">Human-readable description.</param>
/// <param name="VariableIds">Ids of the leaf variables, including any reached through the other two lists.</param>
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
    IReadOnlyList<string> DerivedExpressionIds,
    IReadOnlyList<string> RelationshipIds);
