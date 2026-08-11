using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement;

namespace Calcusystem.Analysis;

/// <summary>
/// What one evaluation of a system produced: the values it resolved, and what stopped it resolving the rest.
/// </summary>
/// <remarks>
/// <para>
/// A snapshot, not a live view. It is a pure function of the system and the bindings it was evaluated with, and
/// holds no reference back into the graph's mutable state — later assignments to a <see cref="Variable"/> do not
/// change it, and re-running is how you get a newer one.
/// </para>
/// <para>
/// <see cref="Values"/> is keyed by node id and covers every node reached, not just the ones the system lists.
/// That makes it the natural place for caching to land: within a run it already means a shared sub-expression is
/// computed once, and across runs it is what a staleness check would be able to reuse. Nothing is cached on the
/// nodes themselves, which is why a node can be asked for its value at any time without a stale answer.
/// </para>
/// </remarks>
/// <param name="Values">Every node that resolved, by id.</param>
/// <param name="Unresolved">The system's own expressions that could not be computed.</param>
/// <param name="MissingValues">The unbound variables responsible — supply these and more will resolve.</param>
public sealed record EvaluationResult(
    IReadOnlyDictionary<string, Measurand> Values,
    IReadOnlyList<IExpression> Unresolved,
    IReadOnlyList<Variable> MissingValues)
{
    /// <summary>Whether every expression the system lists produced a value.</summary>
    public bool IsComplete => Unresolved.Count == 0;

    /// <summary>The value computed for <paramref name="expression"/>, or null if it did not resolve.</summary>
    public Measurand? ValueOf(IExpression expression) =>
        Values.TryGetValue(expression.Id, out var value) ? value : null;
}
