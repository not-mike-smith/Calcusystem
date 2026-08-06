namespace Measurement.Extensions;

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

    public static RelativeError Fraction(this double relativeError)
    {
        return new RelativeError(relativeError);
    }

    public static RelativeError Percent(this double relativeErrorPercent)
    {
        return new RelativeError(relativeErrorPercent / 100d);
    }
}