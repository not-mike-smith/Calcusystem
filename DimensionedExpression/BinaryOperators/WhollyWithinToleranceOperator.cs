using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;

namespace DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the entire Lhs uncertainty interval [Lhs ± Lhs.error] is strictly contained within
/// the Rhs tolerance band [Rhs ± Rhs.error]. Both the lower and upper bounds of Lhs must lie inside
/// the Rhs interval; the Lhs interval touching the Rhs boundary does not satisfy this operator.
/// Symbol: [=}
/// Use for worst-case bilateral conformance checks where no part of the measurement's uncertainty range
/// may fall outside the specification.
/// </summary>
public class WhollyWithinToleranceOperator : NonCommutativeOperatorBase
{
    public override string Symbol => "[=}";

    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
        {
            return null;
        }

        var testValue = Lhs.Value!;
        var bindingValue = Rhs.Value!;
        var lowerBoundWithinTolerance = testValue.KmsValue - testValue.KmsLowerAbsoluteError >
                                        bindingValue.KmsValue - bindingValue.KmsLowerAbsoluteError;

        var upperBoundWithinTolerance = testValue.KmsValue + testValue.KmsUpperAbsoluteError <
                                        bindingValue.KmsValue + bindingValue.KmsUpperAbsoluteError;

        return lowerBoundWithinTolerance && upperBoundWithinTolerance;
    }
}
