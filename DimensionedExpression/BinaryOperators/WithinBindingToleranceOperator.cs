using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the Lhs nominal (point) value falls within the Rhs tolerance band.
/// The Lhs uncertainty is ignored; only the central value is tested.
/// <br/><br/>
/// Symbol: <b>·=}</b>
/// </summary>
public class WithinBindingToleranceOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorType Type => BinaryOperatorType.WithinBindingTolerance;

    public override string Symbol => "·=}";

    /// <summary><inheritdoc/></summary>
    /// <remarks>The containment ladder's <c>NominalWithin</c> rung.</remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
    [
        new(Landmark.Nominal, MustBe.GreaterThanOrEqualTo, Landmark.LowerBound),
        new(Landmark.Nominal, MustBe.LessThanOrEqualTo, Landmark.UpperBound),
    ];
}

/// <summary>
/// Satisfied when the Lhs nominal value is at or above the Rhs lower bound AND the Lhs upper uncertainty
/// bound does not exceed the Rhs upper bound. In other words, the test value is in range and cannot
/// overshoot the upper limit even in the worst case.
/// <br/><br/>
/// Symbol: <b>·⌜=}</b>
/// </summary>
/// <remarks>
/// Use for maximum-value constraints where the measurement's uncertainty must not push it over the limit
/// (e.g. a maximum current or temperature rating).
/// </remarks>
public class PointAndUpperBoundWithinToleranceOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorType Type => BinaryOperatorType.PointAndUpperBoundWithinTolerance;

    public override string Symbol => "·⌜=}";

    /// <summary><inheritdoc/></summary>
    /// <remarks>
    /// The floor is tested against the nominal value rather than against the subject's own floor: this operator
    /// bounds the worst case upward only, and demanding the subject's floor clear the band's would make it the
    /// stricter bilateral check instead.
    /// </remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
    [
        new(Landmark.Nominal, MustBe.GreaterThanOrEqualTo, Landmark.LowerBound),
        new(Landmark.UpperBound, MustBe.LessThanOrEqualTo, Landmark.UpperBound),
    ];
}

/// <summary>
/// Satisfied when the Lhs nominal value is at or below the Rhs upper bound AND the Lhs lower uncertainty
/// bound does not go below the Rhs lower bound. In other words, the test value is in range and cannot
/// undershoot the lower limit even in the worst case.
/// <br/><br/>
/// Symbol: <b>·⌞=}</b>
/// </summary>
/// <remarks>
/// Use for minimum-value constraints where the measurement's uncertainty must not pull it below the floor
/// (e.g. a minimum flow rate or yield strength).
/// </remarks>
public class PointAndLowerBoundWithinToleranceOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorType Type => BinaryOperatorType.PointAndLowerBoundWithinTolerance;

    public override string Symbol => "·⌞=}";

    /// <summary><inheritdoc/></summary>
    /// <remarks>The mirror of <see cref="PointAndUpperBoundWithinToleranceOperator"/>, bounding downward only.</remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
    [
        new(Landmark.Nominal, MustBe.LessThanOrEqualTo, Landmark.UpperBound),
        new(Landmark.LowerBound, MustBe.GreaterThanOrEqualTo, Landmark.LowerBound),
    ];
}
