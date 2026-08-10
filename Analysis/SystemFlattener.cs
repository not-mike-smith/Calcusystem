using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.DimensionedExpression.Traversal;
using Calcusystem.Measurement;

namespace Calcusystem.Analysis;

/// <summary>
/// Reduces an <see cref="ExpressionSystem"/> to the <see cref="FlatSystem"/> its degrees of freedom are computed
/// from.
/// </summary>
public static class SystemFlattener
{
    /// <summary>
    /// Flattens <paramref name="system"/> into its unknowns and equations.
    /// </summary>
    /// <param name="system">The system to analyse.</param>
    /// <param name="bindings">
    /// Values supplied for the duration of this analysis, keyed by variable id — a variable named here is not an
    /// unknown, whatever its own <c>Value</c> says. Only the keys affect degrees of freedom; the measurands
    /// matter to an evaluator working from the same argument. This is how an over-determined system is
    /// interrogated: pin different subsets and compare what each one resolves to.
    /// </param>
    /// <remarks>
    /// Unknowns are gathered from the leaves the system actually reaches — its declared variables, plus those
    /// reachable through its derived expressions and through both sides of its relationships. A variable
    /// referenced only by a derived expression still counts: nothing can produce that expression's value until
    /// it is supplied.
    /// </remarks>
    public static FlatSystem Flatten(
        ExpressionSystem system,
        IReadOnlyDictionary<string, Measurand>? bindings = null)
    {
        bool IsUnknown(Variable v) => bindings is null || bindings.ContainsKey(v.Id) is false;

        var unknowns = system.DirectExpressions
            .Where(v => v.IsFullyDescribed is false)
            .Concat(system.DerivedExpressions.SelectMany(e => e.FreeVariables()))
            .Concat(system.Relationships.SelectMany(r => r.FreeVariables()))
            .Where(IsUnknown)
            .DistinctBy(v => v.Id)
            .ToList();

        // Only determining relationships are equations. A tolerance or ordering relation constrains a value to
        // an interval, which no solver can turn into a point, so counting one here would claim a degree of
        // freedom had been removed when it had not.
        var equations = system.Definitions
            .Select(r => new Equation(r, r.FreeVariables().Where(IsUnknown).DistinctBy(v => v.Id).ToList()))
            .ToList();

        return new FlatSystem(unknowns, equations);
    }
}
