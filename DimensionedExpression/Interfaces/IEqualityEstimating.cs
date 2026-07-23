using Measurement;

namespace DimensionedExpression.Interfaces;

public interface IEqualityEstimating
{
    bool AreEqual(Measurand lhs, Measurand rhs);
}
