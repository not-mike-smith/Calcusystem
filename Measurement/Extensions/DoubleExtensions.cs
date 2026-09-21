using Calcusystem.Measurement.Uncertainties;

namespace Calcusystem.Measurement.Extensions;

public static class DoubleExtensions
{
    public static double SafeDivide(this double numerator, double denominator)
    {
        if (denominator == 0d)
        {
            return double.PositiveInfinity;
        }

        return numerator / denominator;
    }

    public static double RootSumOfSquares(this IEnumerable<double> values)
    {
        double sumOfSquares = 0d;
        foreach (var value in values)
        {
            sumOfSquares += value * value;
        }

        return Math.Sqrt(sumOfSquares);
    }

    public static double RootSumOfSquares<T>(this IEnumerable<T> values, Func<T, double> selector)
    {
        double sumOfSquares = 0d;
        foreach (var value in values)
        {
            var selectedValue = selector(value);
            sumOfSquares += selectedValue * selectedValue;
        }

        return Math.Sqrt(sumOfSquares);
    }

    public static RelativeUncertainty Fraction(this double relativeUncertainty)
    {
        return new RelativeUncertainty(relativeUncertainty);
    }

    public static RelativeUncertainty Percent(this double relativeUncertaintyPercent)
    {
        return new RelativeUncertainty(relativeUncertaintyPercent / 100d);
    }

    /// <summary>
    /// Whether an ordering between <paramref name="lhs"/> and <paramref name="rhs"/> is meaningful at all.
    /// </summary>
    /// <remarks>
    /// Three cases are not. A <see cref="double.NaN"/> compares false against everything including itself, so
    /// no ordering holds and none is denied. Two infinities of the same sign are worse than unordered — IEEE
    /// reports them equal, but they stand for "grew without bound", which says nothing about whether one
    /// outgrew the other. Answering "equal" there would manufacture agreement out of two unknowns.
    /// <para>
    /// Infinities of <i>opposite</i> sign are deliberately absent: those are perfectly ordered, and a bounded
    /// value against either is ordered too.
    /// </para>
    /// </remarks>
    public static bool IsComparisonDetermined(this double lhs, double rhs)
    {
        if (double.IsNaN(lhs) || double.IsNaN(rhs)) return false;
        if (double.IsPositiveInfinity(lhs) && double.IsPositiveInfinity(rhs)) return false;
        if (double.IsNegativeInfinity(lhs) && double.IsNegativeInfinity(rhs)) return false;

        return true;
    }
}
