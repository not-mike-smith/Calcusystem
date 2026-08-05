using DimensionedExpression.Interfaces;

using DimensionedExpression.BaseModels;

namespace DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the Lhs and Rhs are judged equal by the injected <see cref="IEqualityEstimating"/> strategy,
/// which decides what "equal enough" means given each side's uncertainty (exact equality is rarely meaningful
/// for measured values).
/// <br/>
/// Symbol: <b>==</b>
/// <br/>
/// Use when two quantities are expected to be the same and you want a pluggable notion of equality rather than
/// a fixed tolerance rule.
/// </summary>
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
