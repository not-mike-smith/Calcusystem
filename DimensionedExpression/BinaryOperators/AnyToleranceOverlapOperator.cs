using DimensionedExpression.BaseModels;

namespace DimensionedExpression.BinaryOperators;

public class AnyToleranceOverlapOperator : CommutativeOperatorBase
{
    public override string Symbol => "≈";

    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
        {
            return null;
        }

        var (smallerValue, biggerValue) = Lhs.Value!.KmsValue < Rhs.Value!.KmsValue
            ? (Lhs.Value!, Rhs.Value!)
            : (Rhs.Value!, Lhs.Value!);

        var smallerValuePlusError = smallerValue.KmsValue + smallerValue.KmsAbsoluteError;
        var biggerValueMinusError = biggerValue.KmsValue - biggerValue.KmsAbsoluteError;
        return smallerValuePlusError >= biggerValueMinusError;
    }
}
