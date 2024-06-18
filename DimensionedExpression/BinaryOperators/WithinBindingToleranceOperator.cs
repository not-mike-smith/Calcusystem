using DimensionedExpression.BaseModels;

namespace DimensionedExpression.BinaryOperators;

public class WithinBindingToleranceOperator : NonCommutativeOperatorBase
{
    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
        {
            return null;
        }

        var testValue = Lhs.Value!;
        var bindingValue = Rhs.Value!;
        var bindingLowerBound = bindingValue.KmsValue - bindingValue.KmsAbsoluteError;
        var bindingUpperBound = bindingValue.KmsValue + bindingValue.KmsAbsoluteError;
        var isWithinTolerance = testValue.KmsValue >= bindingLowerBound && testValue.KmsValue <= bindingUpperBound;
        return isWithinTolerance;
    }

    public override string Symbol => "=}";
}

public class WithinToleranceAndNotOver : NonCommutativeOperatorBase
{
    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
        {
            return null;
        }

        var testValue = Lhs.Value!;
        var bindingValue = Rhs.Value!;
        var bindingLowerBound = bindingValue.KmsValue - bindingValue.KmsAbsoluteError;
        var bindingUpperBound = bindingValue.KmsValue + bindingValue.KmsAbsoluteError;
        var isWithinTolerance = testValue.KmsValue >= bindingLowerBound;
        var isNotOverTolerance = testValue.KmsValue + testValue.KmsAbsoluteError <= bindingUpperBound;
        return isWithinTolerance && isNotOverTolerance;
    }

    public override string Symbol => "[≓}";
}

public class WithinToleranceAndNotUnder : NonCommutativeOperatorBase
{
    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
        {
            return null;
        }

        var testValue = Lhs.Value!;
        var bindingValue = Rhs.Value!;
        var bindingLowerBound = bindingValue.KmsValue - bindingValue.KmsAbsoluteError;
        var bindingUpperBound = bindingValue.KmsValue + bindingValue.KmsAbsoluteError;
        var isWithinTolerance = testValue.KmsValue <= bindingUpperBound;
        var isNotUnderTolerance = testValue.KmsValue - testValue.KmsAbsoluteError >= bindingLowerBound;
        return isWithinTolerance && isNotUnderTolerance;
    }

    public override string Symbol => "[≒}";
}
