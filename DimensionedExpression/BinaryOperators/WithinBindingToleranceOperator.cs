
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.Measurement;

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

    public override bool IsSatisfiedGiven(Measurand lhs, Measurand rhs) =>
        ContainmentLadder.Evaluate(lhs, rhs).NominalWithin;

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

    public override bool IsSatisfiedGiven(Measurand lhs, Measurand rhs) =>
        ContainmentLadder.Evaluate(lhs, rhs).NominalAndUpperWithin;

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

    public override bool IsSatisfiedGiven(Measurand lhs, Measurand rhs) =>
        ContainmentLadder.Evaluate(lhs, rhs).NominalAndLowerWithin;

    public override string Symbol => "[≒}";
}
