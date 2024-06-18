using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;

namespace DimensionedExpression.BinaryOperators;

public class WhollyWithinToleranceOperator : NonCommutativeOperatorBase
{
    public override string Symbol =>"[=}";

    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
        {
            return null;
        }

        var testValue = Lhs.Value!;
        var bindingValue = Rhs.Value!;
        var lowerBoundWithinTolerance = testValue.KmsValue - testValue.KmsAbsoluteError >
                                        bindingValue.KmsValue - bindingValue.KmsAbsoluteError;

        var upperBoundWithinTolerance = testValue.KmsValue + testValue.KmsAbsoluteError <
                                        bindingValue.KmsValue + bindingValue.KmsAbsoluteError;

        return lowerBoundWithinTolerance && upperBoundWithinTolerance;
    }
}
