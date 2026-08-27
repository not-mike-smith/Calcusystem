using Calcusystem.Measurement;

using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.BaseModels;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when each side's nominal value falls within the other side's tolerance band — i.e.
/// Lhs ∈ [Rhs ± Rhs.error] AND Rhs ∈ [Lhs ± Lhs.error]. The check is symmetric.
/// <br/>
/// Symbol: <b>≃</b>
/// <br/>
/// Use when two independently measured quantities are expected to agree within their own stated uncertainties.
/// </summary>
public class MutuallyWithinToleranceOperator : CommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.MutuallyWithinTolerance;

    public override string Symbol => "≃";

    public override bool IsSatisfiedGiven(Measurand lhs, Measurand rhs)
    {
        return IsWithinTolerance(lhs, rhs) && IsWithinTolerance(rhs, lhs);
    }

    private bool IsWithinTolerance(Measurand x, Measurand y)
    {
        return x.KmsValue >= y.KmsValue - y.KmsLowerAbsoluteError &&
               x.KmsValue <= y.KmsValue + y.KmsUpperAbsoluteError;
    }
}
