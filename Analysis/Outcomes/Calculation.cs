using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Analysis.Outcomes;

/// <summary>
/// One calculation of a system: the values it was given, the values it produced, and what it could not reach.
/// </summary>
/// <remarks>
/// A record of one run, not a live view: every <see cref="Measurand"/> here is an immutable value, so later
/// assignments to a <see cref="Variable"/> do not change it. Re-run to get a newer one.
/// </remarks>
/// <param name="Overrides">The values supplied for this calculation, which took precedence over stored ones.</param>
/// <param name="Values">Every node that resolved.</param>
/// <param name="Unresolved">
/// The expressions the system contains that could not be computed. That includes both operands of every
/// relationship, since a check whose bound cannot be evaluated is as outstanding as a value that will not resolve.
/// </param>
/// <param name="MissingValues">The unset variables responsible — supply these and more will resolve.</param>
/// <param name="Outcomes">
/// What each of the system's relationships did — one entry per relationship, including the ones that could not
/// be judged.
/// </param>
public sealed record Calculation(
    IReadOnlyDictionary<Variable, Measurand> Overrides,
    IReadOnlyDictionary<IExpression, Measurand> Values,
    IReadOnlyList<IExpression> Unresolved,
    IReadOnlyList<Variable> MissingValues,
    IReadOnlyList<RelationshipOutcome> Outcomes)
{
    /// <summary>Whether every expression the system references produced a value.</summary>
    /// <remarks>
    /// About <i>values</i> only. A calculation whose requirement was violated is still complete, and has a
    /// finding; <see cref="AllRelationshipsHold"/> is the other question.
    /// </remarks>
    public bool IsComplete => Unresolved.Count == 0;

    /// <summary>The value computed for <paramref name="expression"/>, or null if it did not resolve.</summary>
    public Measurand? ValueOf(IExpression expression) =>
        Values.TryGetValue(expression, out var value) ? value : null;

    /// <summary>What <paramref name="relationship"/> did, or null if it is not one this system carries.</summary>
    public RelationshipOutcome? OutcomeFor(IBinaryOperator relationship) =>
        Outcomes.FirstOrDefault(o => o.Relationship.Equals(relationship));

    /// <summary>
    /// Requirements that did not hold — a value outside a bound it was tested against.
    /// </summary>
    public IEnumerable<RelationshipOutcome> Violations => Outcomes.Where(o => o.IsViolation);

    /// <summary>
    /// Equations and coherence assertions that did not hold. Unlike a <see cref="Violations">violation</see>,
    /// nothing here identifies a side at fault: the finding is against the model or its inputs.
    /// </summary>
    public IEnumerable<RelationshipOutcome> Inconsistencies => Outcomes.Where(o => o.IsInconsistency);

    /// <summary>
    /// Relationships that could not be judged because a side did not resolve — outstanding, not passing.
    /// </summary>
    public IEnumerable<RelationshipOutcome> Undetermined => Outcomes.Where(o => o.IsUndetermined);

    /// <summary>Whether every relationship that could be judged held.</summary>
    /// <remarks>
    /// Independent of <see cref="IsComplete"/> in both directions: a fully resolved calculation can fail its
    /// checks, and a half-built model can already have a violation worth reporting.
    /// </remarks>
    public bool AllRelationshipsHold => ! Outcomes.Any(o => o.IsSatisfied is false);
}
