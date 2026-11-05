using System.Globalization;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Units;

namespace Calcusystem.Examples.Presentation;

/// <summary>
/// Renders a <see cref="Measurand"/> as a line of a report: a value in a unit, and the uncertainty on it.
/// </summary>
/// <remarks>
/// Deliberately not the significant-figure convention the editor uses. That rule chooses a figure count from
/// the uncertainty and lives in <c>web/src/format/significance.ts</c>; a second implementation of it here would
/// be a second thing to keep right. A console report wants a fixed width and nothing else, so it fixes the
/// figure count instead.
/// </remarks>
public static class Figures
{
    private const int ValueFigures = 4;
    private const int UncertaintyFigures = 2;

    /// <summary>The value in <paramref name="unit"/>, with its uncertainty as a percentage.</summary>
    /// <remarks>
    /// Falls back to an absolute uncertainty where the relative one is not a number — which is what a value of
    /// zero gives, since a fraction of nothing is undefined.
    /// </remarks>
    public static string Measure(Measurand measurand, UnitOfMeasure unit)
    {
        var value = $"{Significant(measurand.In(unit), ValueFigures)}{Suffix(unit)}";
        var relative = measurand.RelativeUncertainty;

        if (relative == 0) return $"{value} (exact)";

        if (! double.IsFinite(relative))
        {
            var absolute = Significant(measurand.AbsoluteUncertaintyIn(unit), UncertaintyFigures);
            return $"{value} ± {absolute}{Suffix(unit)}";
        }

        return $"{value} ± {Significant(relative * 100, UncertaintyFigures)} %";
    }

    /// <summary>The unit's symbol, spaced off the number — or nothing at all for a bare count.</summary>
    /// <remarks>
    /// <c>Dimensionless.One</c> carries the symbol <c>unit</c> so that it has one, which is right for a unit
    /// list and wrong on a page: a discharge coefficient reads as <c>0.6100</c>, not <c>0.6100 unit</c>. Every
    /// other dimensionless unit — a percentage, a dozen — keeps its symbol, because there the symbol is the
    /// whole of what distinguishes it.
    /// </remarks>
    public static string Suffix(UnitOfMeasure unit) =>
        ReferenceEquals(unit, Dimensionless.One) ? "" : $" {unit.Symbol}";

    /// <summary><paramref name="value"/> to <paramref name="figures"/> significant figures.</summary>
    /// <remarks>
    /// Fixed-point where that is readable and scientific otherwise, so a report column does not alternate
    /// between the two for values of the same order.
    /// </remarks>
    public static string Significant(double value, int figures)
    {
        if (value == 0 || ! double.IsFinite(value))
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        var exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
        if (exponent < -4 || exponent >= figures + 3)
        {
            return value.ToString("G" + figures, CultureInfo.InvariantCulture);
        }

        return value.ToString("F" + Math.Max(0, figures - 1 - exponent), CultureInfo.InvariantCulture);
    }
}
