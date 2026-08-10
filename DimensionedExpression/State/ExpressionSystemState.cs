namespace DimensionedExpression.State;

/// <summary>
/// The complete stored state of an <see cref="Systems.ExpressionSystem"/>: its identity, its labels, and the ids
/// of everything it contains.
/// </summary>
/// <remarks>
/// The system is the one node whose references are not all the same type — expressions in two of its lists,
/// operators in the third. That is why node resolution is a per-reference query rather than a single typed
/// delegate; without it this type would have needed a bespoke assembly path.
/// </remarks>
/// <param name="Id">Stable identity.</param>
/// <param name="Name">Human-readable name.</param>
/// <param name="Description">Human-readable description.</param>
/// <param name="DirectExpressionIds">Ids of the mutable leaf variables.</param>
/// <param name="DerivedExpressionIds">Ids of the computed expressions.</param>
/// <param name="RelationshipIds">
/// Ids of every asserted relationship, definitions and constraints alike. They share one list because which one
/// a relationship is, is carried by the operator's own <c>IsDetermining</c> — storing it as list membership too
/// would let the two disagree.
/// </param>
public readonly record struct ExpressionSystemState(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string> DirectExpressionIds,
    IReadOnlyList<string> DerivedExpressionIds,
    IReadOnlyList<string> RelationshipIds);
