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

    /// <remarks>
    /// The nominal containment rung applied in both directions — a quantifier variation on the ladder rather
    /// than a rung of its own, which is why this operator has no unique arithmetic left.
    /// </remarks>
    public override bool IsSatisfiedGiven(Measurand lhs, Measurand rhs) =>
        ContainmentLadder.Evaluate(lhs, rhs).NominalWithin &&
        ContainmentLadder.Evaluate(rhs, lhs).NominalWithin;
}
