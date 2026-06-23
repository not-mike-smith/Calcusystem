using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;

namespace DimensionedExpression.BinaryOperators;

public class EqualityOperator(IEqualityEstimating equalityEstimator) : CommutativeOperatorBase
{
    public override string Symbol => "==";

    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
            return null;

        return equalityEstimator.AreEqual(Lhs.Value!, Rhs.Value!);
    }
}
