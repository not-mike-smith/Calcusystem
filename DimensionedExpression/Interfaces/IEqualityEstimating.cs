using Measurement.BaseClasses;

namespace DimensionedExpression.Interfaces;

public interface IEqualityEstimating
{
    bool AreEqual(PrecisionQuantity lhs, PrecisionQuantity rhs);
}
