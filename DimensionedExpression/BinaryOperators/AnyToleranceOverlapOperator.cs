
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.DimensionedExpression.Traversal;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the Lhs and Rhs tolerance bands overlap at all — i.e. there exists at least one value
/// that is consistent with both uncertainties. This is the weakest form of agreement: even a single
/// shared point in the two intervals is sufficient.
/// <br/>
/// Symbol: <b>≈</b>
/// <br/>
/// Use when checking whether two measurements are at least plausibly compatible, without requiring
/// that one falls squarely within the other's band.
/// </summary>
public class AnyToleranceOverlapOperator : CommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.AnyToleranceOverlap;

    public override string Symbol => "≈";

    public override bool? IsSatisfied()
    {
        // One walk per side. `CalculateValueIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.CalculateValueIfDetermined();
        var rhs = Rhs.CalculateValueIfDetermined();
        if (lhs is null || rhs is null) return null;

        var (smallerValue, biggerValue) = lhs.KmsValue < rhs.KmsValue
            ? (lhs, rhs)
            : (rhs, lhs);

        var smallerValuePlusError = smallerValue.KmsValue + smallerValue.KmsUpperAbsoluteError;
        var biggerValueMinusError = biggerValue.KmsValue - biggerValue.KmsLowerAbsoluteError;
        return smallerValuePlusError >= biggerValueMinusError;
    }
}
