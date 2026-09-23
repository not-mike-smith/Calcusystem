using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;

namespace Calcusystem.Analysis.Outcomes;

/// <summary>
/// One determining relationship, paired with the unknowns it is incident on.
/// </summary>
/// <remarks>
/// The incident set comes from walking both sides, so a computed node between the operator and a leaf
/// contributes nothing of its own — it is the path by which the equation reaches that leaf.
/// </remarks>
/// <param name="Relationship">The determining operator this row stands for.</param>
/// <param name="Unknowns">The distinct unknowns reachable from either side.</param>
public sealed record Equation(IBinaryOperator Relationship, IReadOnlyList<Variable> Unknowns);
