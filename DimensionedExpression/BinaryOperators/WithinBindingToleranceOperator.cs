using DimensionedExpression.BaseModels;

namespace DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the Lhs nominal (point) value falls within the Rhs tolerance band.
/// The Lhs uncertainty is ignored; only the central value is tested.
/// Symbol: =}
/// Use when a single measurement must fall within a specified range, regardless of its own uncertainty.
/// </summary>
public class WithinBindingToleranceOperator : NonCommutativeOperatorBase
{
    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
            return null;

        var testValue = Lhs.Value!;
        var bindingValue = Rhs.Value!;
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
/// Symbol: [≓}
/// Use for maximum-value constraints where the measurement's uncertainty must not push it over the limit
/// (e.g. a maximum current or temperature rating).
/// </summary>
public class PointAndUpperBoundWithinToleranceOperator : NonCommutativeOperatorBase
{
    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
            return null;

        var testValue = Lhs.Value!;
        var bindingValue = Rhs.Value!;
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
/// Symbol: [≒}
/// Use for minimum-value constraints where the measurement's uncertainty must not pull it below the floor
/// (e.g. a minimum flow rate or yield strength).
/// </summary>
public class PointAndLowerBoundWithinToleranceOperator : NonCommutativeOperatorBase
{
    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
            return null;

        var testValue = Lhs.Value!;
        var bindingValue = Rhs.Value!;
        var isBelowUpperBound = testValue.KmsValue <= bindingValue.KmsValue + bindingValue.KmsUpperAbsoluteError;
        var lowerBoundNotViolated =
            testValue.KmsValue - testValue.KmsLowerAbsoluteError >=
            bindingValue.KmsValue - bindingValue.KmsLowerAbsoluteError;
        return isBelowUpperBound && lowerBoundNotViolated;
    }

    public override string Symbol => "[≒}";
}
