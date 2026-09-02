using Calcusystem.Analysis.Outcomes;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Analysis.Extensions;

/// <summary>
/// Calculating a system: working out everything its current values and relationships determine.
/// </summary>
/// <remarks>
/// An extension rather than a method on <see cref="ExpressionSystem"/> so that it reads as one
/// (<c>system.Calculate()</c>) without the expression layer having to know about this one. That layer assembles
/// and describes a graph and orchestrates nothing; keeping the dependency pointing this way is also what lets a
/// second strategy — a solver, an interval evaluator — sit beside this one rather than inside the domain type.
/// </remarks>
public static class SystemCalculation
{
    private static readonly IReadOnlyDictionary<Variable, Measurand> _emptyOverrides = new Dictionary<Variable, Measurand>();

    /// <summary>
    /// Calculates every expression in <paramref name="system"/>, resolving what it can and reporting what it
    /// could not.
    /// </summary>
    /// <param name="system">The system to calculate. Never mutated.</param>
    /// <param name="overrides">
    /// Values supplied for this calculation only, taking precedence over a variable's own. This is how a caller
    /// calculates at trial values without writing them into the model — see the assembly README.
    /// </param>
    /// <param name="propagator">
    /// How uncertainties are combined, or null for the conservative Gaussian default. Orthogonal to a node's own
    /// <c>UncertaintyCorrelation</c>: that records whether particular operands are correlated, which is a fact about
    /// the model and is not this calculation's to overrule, while this is the numerical method for combining
    /// uncertainties at all. Supplying a Monte Carlo propagator therefore re-runs the same model under a
    /// different uncertainty treatment rather than discarding what the model says.
    /// </param>
    /// <remarks>
    /// Each node is computed once: nodes are visited in dependency order and handed the values already
    /// established, so a sub-expression shared by three parents is computed once rather than three times as
    /// <c>ComputeIfFullyDescribed()</c> would.
    /// </remarks>
    public static Calculation Calculate(
        this ExpressionSystem system,
        IReadOnlyDictionary<Variable, Measurand>? overrides = null,
        IUncertaintyPropagator? propagator = null)
    {
        overrides ??= _emptyOverrides;

        var contained = system.GetAllExpressions().ToList();

        // Seeded with the overrides, so a variable that has one finds itself already answered. Nothing here
        // needs to know a leaf from a composite — `ComputeFrom` is where that distinction lives.
        var values = new Dictionary<IExpression, Measurand>();
        foreach (var (variable, value) in overrides) values[variable] = value;

        foreach (var node in system.InDependencyOrder())
        {
            // Children come first in this ordering, so anything absent from `values` is beneath an unset
            // leaf. `ComputeFrom` answers null in that case rather than throwing, so no pre-check is needed.
            if (node.ComputeFrom(values, propagator) is { } value)
            {
                values[node] = value;
            }
        }

        var unresolved = contained.Where(e => ! values.ContainsKey(e)).ToList();

        // Read straight off the system's variables rather than re-deriving them per node. `Variables` already
        // holds every variable the system reaches, so asking each expression for its free variables would walk
        // the same subgraphs again — once per node, which is quadratic on a deep graph.
        var missing = system.Variables
            .Where(v => ! v.IsFullyDescribed && ! overrides.ContainsKey(v))
            .ToList();

        var outcomes = system.Relationships.Select(r => Judge(r, values)).ToList();

        return new Calculation(overrides, values, unresolved, missing, outcomes);
    }

    /// <summary>
    /// Reads a relationship's two operands out of what has already been computed and asks the operator's
    /// predicate about them.
    /// </summary>
    /// <remarks>
    /// The reason verdicts are produced here rather than by calling <c>relationship.IsSatisfied()</c>. That
    /// would re-walk both subgraphs this calculation has just finished walking, twice per relationship, and —
    /// worse — it would resolve them against the <i>stored</i> model, so a calculation run at trial values would
    /// quietly report checks against values it was told to ignore. Both problems disappear by handing the
    /// operator the values instead of letting it fetch them.
    /// </remarks>
    private static RelationshipOutcome Judge(
        IBinaryOperator relationship,
        IReadOnlyDictionary<IExpression, Measurand> values)
    {
        var lhs = values.GetValueOrDefault(relationship.Lhs);
        var rhs = values.GetValueOrDefault(relationship.Rhs);

        // Undetermined rather than failed. A check whose operands did not resolve has not been run, and
        // reporting it as false would manufacture a finding out of a missing value.
        var verdict = lhs is not null && rhs is not null
            ? relationship.IsSatisfiedGiven(lhs, rhs)
            : (bool?)null;

        return new RelationshipOutcome(relationship, verdict, lhs, rhs);
    }
}
