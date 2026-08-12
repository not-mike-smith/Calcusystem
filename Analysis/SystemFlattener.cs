using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement;

namespace Calcusystem.Analysis;

/// <summary>
/// Reduces an <see cref="ExpressionSystem"/> to the <see cref="FlatSystem"/> its degrees of freedom are computed
/// from.
/// </summary>
/// <remarks>
/// An extension rather than a method on <see cref="ExpressionSystem"/> so that it reads as one
/// (<c>system.Flatten()</c>) without the expression layer having to know about this one — the same arrangement
/// as <c>Calculate</c>, and for the same reason.
/// </remarks>
public static class SystemFlattener
{
    /// <summary>
    /// Flattens <paramref name="system"/> into its unknowns and equations.
    /// </summary>
    /// <param name="system">The system to analyse.</param>
    /// <param name="overrides">
    /// Values supplied for the duration of this analysis — a variable named here is not an unknown, whatever its
    /// own <c>Value</c> says. Only the keys affect degrees of freedom; the measurands matter to <c>Calculate</c>
    /// working from the same argument. This is how an over-determined system is interrogated: pin different
    /// subsets and compare what each one resolves to.
    /// </param>
    /// <remarks>
    /// Unknowns are gathered from the leaves the system actually reaches — its declared variables, plus those
    /// reachable through its derived expressions and through both sides of its relationships. A variable
    /// referenced only by a derived expression still counts: nothing can produce that expression's value until
    /// it is supplied.
    /// </remarks>
    public static FlatSystem Flatten(
        this ExpressionSystem system,
        IReadOnlyDictionary<Variable, Measurand>? overrides = null)
    {
        bool IsUnknown(Variable v) => overrides is null || ! overrides.ContainsKey(v);

        var unknowns = system.DirectExpressions
            .Where(v => ! v.IsFullyDescribed)
            .Concat(system.DerivedExpressions.SelectMany(e => e.FreeVariables()))
            .Concat(system.Relationships.SelectMany(r => r.FreeVariables()))
            .Where(IsUnknown)
            .Distinct()
            .ToList();

        // Only determining relationships are equations. A tolerance or ordering relation constrains a value to
        // an interval, which no solver can turn into a point, so counting one here would claim a degree of
        // freedom had been removed when it had not.
        var equations = system.Definitions
            .Select(r => new Equation(r, r.FreeVariables().Where(IsUnknown).Distinct().ToList()))
            .ToList();

        return new FlatSystem(unknowns, equations);
    }
}
