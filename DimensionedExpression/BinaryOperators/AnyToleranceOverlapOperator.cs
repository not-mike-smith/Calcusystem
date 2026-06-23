using DimensionedExpression.BaseModels;

namespace DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the Lhs and Rhs tolerance bands overlap at all — i.e. there exists at least one value
/// that is consistent with both uncertainties. This is the weakest form of agreement: even a single
/// shared point in the two intervals is sufficient.
/// Symbol: ≈
/// Use when checking whether two measurements are at least plausibly compatible, without requiring
/// that one falls squarely within the other's band.
/// </summary>
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

        var smallerValuePlusError = smallerValue.KmsValue + smallerValue.KmsUpperAbsoluteError;
        var biggerValueMinusError = biggerValue.KmsValue - biggerValue.KmsLowerAbsoluteError;
        return smallerValuePlusError >= biggerValueMinusError;
    }
}
