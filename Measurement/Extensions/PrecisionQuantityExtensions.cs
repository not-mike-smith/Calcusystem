using System;
using System.Collections.Generic;
using System.Linq;
using Measurement.BaseClasses;

namespace Measurement.Extensions;

public static class PrecisionQuantityExtensions
{
    public static double SumOfSquares(
        this IEnumerable<PrecisionQuantity> quantities,
        Func<PrecisionQuantity, double> selector)
    {
        return quantities.Sum(q =>
        {
            double x = selector(q);
            return x * x;
        });
    }
}
