using Calcusystem.DimensionedExpression.Interfaces;

using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.BaseModels;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the entire Lhs uncertainty interval [Lhs ± Lhs.error] is strictly contained within
/// the Rhs tolerance band [Rhs ± Rhs.error]. Both the lower and upper bounds of Lhs must lie inside
/// the Rhs interval; the Lhs interval touching the Rhs boundary does not satisfy this operator.
/// <br/>
/// Symbol: <b>[=}</b>
/// <br/>
/// Use for worst-case bilateral conformance checks where no part of the measurement's uncertainty range
/// may fall outside the specification.
/// </summary>
public class WhollyWithinToleranceOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.WhollyWithinTolerance;

    public override string Symbol => "[=}";

    public override bool? IsSatisfied()
    {
        // One walk per side. `ComputeIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.ComputeIfDetermined();
        var rhs = Rhs.ComputeIfDetermined();
        if (lhs is null || rhs is null) return null;

        var testValue = lhs;
        var bindingValue = rhs;
        var lowerBoundWithinTolerance = testValue.KmsValue - testValue.KmsLowerAbsoluteError >
                                        bindingValue.KmsValue - bindingValue.KmsLowerAbsoluteError;

        var upperBoundWithinTolerance = testValue.KmsValue + testValue.KmsUpperAbsoluteError <
                                        bindingValue.KmsValue + bindingValue.KmsUpperAbsoluteError;

        return lowerBoundWithinTolerance && upperBoundWithinTolerance;
    }
}
