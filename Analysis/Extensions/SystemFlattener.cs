using Calcusystem.Analysis.Results;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Quantities;

namespace Calcusystem.Analysis.Extensions;

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
    /// Unknowns are simply the system's unvalued variables. That is a complete answer because
    /// <c>ExpressionSystem.Variables</c> already holds every variable the system reaches, including ones only a
    /// derived expression or a relationship's operand refers to — this used to gather from three places and
    /// deduplicate, which was the same question asked three times.
    /// </remarks>
    public static FlatSystem Flatten(
        this ExpressionSystem system,
        IReadOnlyDictionary<Variable, Measurand>? overrides = null)
    {
        bool IsUnknown(Variable v) => overrides is null || ! overrides.ContainsKey(v);

        var unknowns = system.Variables
            .Where(v => ! v.IsFullyDescribed)
            .Where(IsUnknown)
            .ToList();

        // Only determining relationships are equations. A tolerance or ordering relation constrains a value to
        // an interval, which no solver can turn into a point, so counting one here would claim a degree of
        // freedom had been removed when it had not.
        var equations = system.Relationships.Where(r => r.IsDetermining)
            .Select(r => new Equation(r, r.FreeVariables().Where(IsUnknown).Distinct().ToList()))
            .ToList();

        return new FlatSystem(unknowns, equations);
    }
}
