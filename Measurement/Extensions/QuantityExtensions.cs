using Measurement.Models;

namespace Measurement.Extensions;

public static class QuantityExtensions
{
    public static Quantity Units(this double d, UnitOfMeasure unitOfMeasure)
    {
        return new Quantity(d, unitOfMeasure);
    }
}
