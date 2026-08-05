namespace DimensionedExpression.State;

/// <summary>
/// The complete stored state of an <see cref="Systems.ExpressionSystem"/>: its identity, its labels, and the ids
/// of everything it contains.
/// </summary>
/// <remarks>
/// The system is the one node whose references are not all the same type — expressions in two of its lists,
/// operators in the other two. That is why node resolution is a per-reference query rather than a single typed
/// delegate; without it this type would have needed a bespoke assembly path.
/// </remarks>
/// <param name="Id">Stable identity.</param>
/// <param name="Name">Human-readable name.</param>
/// <param name="Description">Human-readable description.</param>
/// <param name="DirectExpressionIds">Ids of the mutable leaf variables.</param>
/// <param name="DerivedExpressionIds">Ids of the computed expressions.</param>
/// <param name="DefinitionIds">Ids of the always-true relationships used to compute unknowns.</param>
/// <param name="ConstraintIds">Ids of the checks evaluated against values.</param>
public readonly record struct ExpressionSystemState(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string> DirectExpressionIds,
    IReadOnlyList<string> DerivedExpressionIds,
    IReadOnlyList<string> DefinitionIds,
    IReadOnlyList<string> ConstraintIds);
