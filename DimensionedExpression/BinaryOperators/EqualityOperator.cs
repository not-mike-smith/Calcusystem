using DimensionedExpression.BaseModels;

namespace DimensionedExpression.BinaryOperators;

public class EqualityOperator : CommutativeOperatorBase
{
    public override string Symbol => "==";

    public override bool? IsSatisfied()
    {
        throw new NotImplementedException("Use the IEqualityEstimating interface to do DI");
    }
}
