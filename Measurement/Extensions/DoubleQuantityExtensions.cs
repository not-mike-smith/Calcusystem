using System.Collections.Generic;
using System.Linq;
using Measurement.Models;

namespace Measurement.Extensions;

public static class DoubleQuantityExtensions
{
    public static Quantity Units(this double d, UnitOfMeasure unitOfMeasure)
    {
        return new Quantity(d, unitOfMeasure);
    }
}
