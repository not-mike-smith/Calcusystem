using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement;

namespace Calcusystem.Analysis;

/// <summary>
/// One calculation of a system: the values it was given, the values it produced, and what it could not reach.
/// </summary>
/// <remarks>
/// <para>
/// Named for the engineering artefact rather than the operation — a calculation is something an engineer
/// produces, keeps, and hands to a reviewer, and it is defined as much by its inputs as its outputs. That is why
/// <see cref="Overrides"/> is on the record: a bare set of values is not reproducible or reviewable without the
/// assumptions that produced it, and carrying both means two calculations of the same system can be compared on
/// equal terms.
/// </para>
/// <para>
/// A snapshot, not a live view. It is a pure function of the system and <see cref="Overrides"/>, and every
/// <see cref="Measurand"/> in it is an immutable value: later assignments to a <see cref="Variable"/> cannot
/// change what is recorded here, and re-running is how a newer one is obtained.
/// </para>
/// <para>
/// <see cref="Values"/> covers every node reached, not only the ones the system lists, which is what makes it
/// the natural home for caching. Within a run it already means a shared sub-expression is computed once; across
/// runs it is what a staleness check would reuse. Nothing is cached on the nodes themselves, so a node can
/// always be asked directly without risking a stale answer.
/// </para>
/// <para>
/// It reports on the model's relationships as well as its values. Every relationship yields a
/// <see cref="RelationshipOutcome"/>, judged against the values in <see cref="Values"/> — including the
/// <see cref="Overrides"/>, which is the whole reason the verdict is computed here rather than by asking each
/// operator. An operator asked in isolation reads the stored model, so under trial values it would answer a
/// question nobody asked.
/// </para>
/// </remarks>
/// <param name="Overrides">The values supplied for this calculation, which took precedence over stored ones.</param>
/// <param name="Values">Every node that resolved.</param>
/// <param name="Unresolved">
/// The expressions the system contains that could not be computed. That includes both operands of every
/// relationship, since a check whose bound cannot be evaluated is as outstanding as a value that will not resolve.
/// </param>
/// <param name="MissingValues">The unbound variables responsible — supply these and more will resolve.</param>
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
    /// About <i>values</i>, deliberately, and unaffected by whether the checks passed. A calculation in which a
    /// requirement was violated is complete and has a finding — those are different questions, and folding them
    /// together would leave a caller unable to ask the first one.
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
    /// Equations and coherence assertions that did not hold. Distinguished from <see cref="Violations"/> because
    /// nothing here identifies a side at fault: the finding is against the model or its inputs, not against one
    /// operand. This is also what an over-determined system's redundancy checks report through.
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
