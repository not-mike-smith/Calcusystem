using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using Calcusystem.Measurement.Units;

namespace Calcusystem.Examples.Problems;

/// <summary>
/// Builds the valued leaves of a problem — a name, a number in a unit, an uncertainty, and where it came
/// from.
/// </summary>
/// <remarks>
/// The only shorthand in this folder. Every expression tree here is written with the real constructors, because
/// seeing those is the point; a leaf is the one thing repetitive enough to be worth a helper, and its body is
/// the call it replaces.
/// <br/><br/>
/// The name doubles as the id, so a report and a test can name a node the way the problem statement does.
/// </remarks>
internal static class Inputs
{
    /// <summary>A measured or specified value, uncertain by a fraction of itself.</summary>
    public static Variable Known(
        string name,
        double value,
        UnitOfMeasure unit,
        RelativeUncertainty uncertainty,
        IProvenance provenance) =>
        new(name, unit.Quantity(value).WithUncertainty(uncertainty), name) { Provenance = provenance };

    /// <summary>A measured or specified value, uncertain by an amount rather than a fraction.</summary>
    /// <remarks>
    /// Pass the amount in whatever unit reads naturally — <c>0.005.Units(Length.Meter)</c>. It need not be the
    /// unit the value itself was given in.
    /// </remarks>
    public static Variable Known(
        string name,
        double value,
        UnitOfMeasure unit,
        Quantity uncertainty,
        IProvenance provenance) =>
        new(name, unit.Quantity(value).WithUncertainty(uncertainty), name) { Provenance = provenance };

    /// <summary>A value carrying no uncertainty: a defined constant, a pure number, or a stated limit.</summary>
    /// <remarks>
    /// A limit is exact because it is a decision rather than a measurement. The tolerance on the thing being
    /// limited lives on that thing.
    /// </remarks>
    public static Variable Exact(
        string name,
        double value,
        UnitOfMeasure unit,
        IProvenance? provenance = null) =>
        new(name, unit.Quantity(value).WithoutUncertainty(), name) { Provenance = provenance };
}
