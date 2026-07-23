using DimensionedExpression.BaseModels;
using Measurement;

namespace DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when each side's nominal value falls within the other side's tolerance band — i.e.
/// Lhs ∈ [Rhs ± Rhs.error] AND Rhs ∈ [Lhs ± Lhs.error]. The check is symmetric.
/// Symbol: ≃
/// Use when two independently measured quantities are expected to agree within their own stated uncertainties.
/// </summary>
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

    private bool IsWithinTolerance(Measurand x, Measurand y)
    {
        return x.KmsValue >= y.KmsValue - y.KmsLowerAbsoluteError &&
               x.KmsValue <= y.KmsValue + y.KmsUpperAbsoluteError;
    }
}
