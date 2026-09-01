using Calcusystem.Measurement.Quantities;
using Calcusystem.Measurement.Units;

namespace Calcusystem.Measurement.Extensions;

public static class QuantityExtensions
{
    public static Quantity Units(this double d, UnitOfMeasure unitOfMeasure)
    {
        return new Quantity(d, unitOfMeasure);
    }
}
