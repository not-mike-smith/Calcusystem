using DimensionedExpression.BaseModels;
using Measurement.BaseClasses;

namespace DimensionedExpression.BinaryOperators;

public class MutuallyWithinToleranceOperator : CommutativeOperatorBase
{
    public override string Symbol => "≃";

    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
        {
            return null;
        }

        return IsWithinTolerance(Lhs.Value!, Rhs.Value!) && IsWithinTolerance(Rhs.Value!, Lhs.Value!);
    }

    private bool IsWithinTolerance(PrecisionQuantity x, PrecisionQuantity y)
    {
        return x.KmsValue >= y.KmsValue - y.KmsAbsoluteError &&
               x.KmsValue <= y.KmsValue + y.KmsAbsoluteError;
    }
}
