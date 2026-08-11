
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.BaseModels;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the Lhs nominal (point) value falls within the Rhs tolerance band.
/// The Lhs uncertainty is ignored; only the central value is tested.
/// <br/>
/// Symbol: <b>=}</b>
/// <br/>
/// Use when a single measurement must fall within a specified range, regardless of its own uncertainty.
/// </summary>
public class WithinBindingToleranceOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.WithinBindingTolerance;

    public override bool? IsSatisfied()
    {
        // One walk per side. `CalculateValueIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.CalculateValueIfDetermined();
        var rhs = Rhs.CalculateValueIfDetermined();
        if (lhs is null || rhs is null) return null;

        var testValue = lhs;
        var bindingValue = rhs;
        var bindingLowerBound = bindingValue.KmsValue - bindingValue.KmsLowerAbsoluteError;
        var bindingUpperBound = bindingValue.KmsValue + bindingValue.KmsUpperAbsoluteError;
        return testValue.KmsValue >= bindingLowerBound && testValue.KmsValue <= bindingUpperBound;
    }

    public override string Symbol => "=}";
}

/// <summary>
/// Satisfied when the Lhs nominal value is at or above the Rhs lower bound AND the Lhs upper uncertainty
/// bound does not exceed the Rhs upper bound. In other words, the test value is in range and cannot
/// overshoot the upper limit even in the worst case.
/// <br/>
/// Symbol: <b>[≓}</b>
/// <br/>
/// Use for maximum-value constraints where the measurement's uncertainty must not push it over the limit
/// (e.g. a maximum current or temperature rating).
/// </summary>
public class PointAndUpperBoundWithinToleranceOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.PointAndUpperBoundWithinTolerance;

    public override bool? IsSatisfied()
    {
        // One walk per side. `CalculateValueIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.CalculateValueIfDetermined();
        var rhs = Rhs.CalculateValueIfDetermined();
        if (lhs is null || rhs is null) return null;

        var testValue = lhs;
        var bindingValue = rhs;
        var isAboveLowerBound = testValue.KmsValue >= bindingValue.KmsValue - bindingValue.KmsLowerAbsoluteError;
        var upperBoundNotExceeded =
            testValue.KmsValue + testValue.KmsUpperAbsoluteError <=
            bindingValue.KmsValue + bindingValue.KmsUpperAbsoluteError;
        return isAboveLowerBound && upperBoundNotExceeded;
    }

    public override string Symbol => "[≓}";
}

/// <summary>
/// Satisfied when the Lhs nominal value is at or below the Rhs upper bound AND the Lhs lower uncertainty
/// bound does not go below the Rhs lower bound. In other words, the test value is in range and cannot
/// undershoot the lower limit even in the worst case.
/// <br/>
/// Symbol: <b>[≒}</b>
/// <br/>
/// Use for minimum-value constraints where the measurement's uncertainty must not pull it below the floor
/// (e.g. a minimum flow rate or yield strength).
/// </summary>
public class PointAndLowerBoundWithinToleranceOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.PointAndLowerBoundWithinTolerance;

    public override bool? IsSatisfied()
    {
        // One walk per side. `CalculateValueIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.CalculateValueIfDetermined();
        var rhs = Rhs.CalculateValueIfDetermined();
        if (lhs is null || rhs is null) return null;

        var testValue = lhs;
        var bindingValue = rhs;
        var isBelowUpperBound = testValue.KmsValue <= bindingValue.KmsValue + bindingValue.KmsUpperAbsoluteError;
        var lowerBoundNotViolated =
            testValue.KmsValue - testValue.KmsLowerAbsoluteError >=
            bindingValue.KmsValue - bindingValue.KmsLowerAbsoluteError;
        return isBelowUpperBound && lowerBoundNotViolated;
    }

    public override string Symbol => "[≒}";
}
