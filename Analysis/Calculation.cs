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
/// </remarks>
/// <param name="Overrides">The values supplied for this calculation, which took precedence over stored ones.</param>
/// <param name="Values">Every node that resolved.</param>
/// <param name="Unresolved">The system's own expressions that could not be computed.</param>
/// <param name="MissingValues">The unbound variables responsible — supply these and more will resolve.</param>
public sealed record Calculation(
    IReadOnlyDictionary<Variable, Measurand> Overrides,
    IReadOnlyDictionary<IExpression, Measurand> Values,
    IReadOnlyList<IExpression> Unresolved,
    IReadOnlyList<Variable> MissingValues)
{
    /// <summary>Whether every expression the system lists produced a value.</summary>
    public bool IsComplete => Unresolved.Count == 0;

    /// <summary>The value computed for <paramref name="expression"/>, or null if it did not resolve.</summary>
    public Measurand? ValueOf(IExpression expression) =>
        Values.TryGetValue(expression, out var value) ? value : null;
}
