using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the Lhs and Rhs tolerance bands overlap at all — i.e. there exists at least one value
/// that is consistent with both uncertainties. This is the weakest form of agreement. Operator is commutative.
/// <br/><br/>
/// Symbol: <b>{&gt;&lt;}</b>
/// </summary>
public class AnyToleranceOverlapOperator : CommutativeOperatorBase
{
    protected override BinaryOperatorType Type => BinaryOperatorType.AnyToleranceOverlap;

    /// <inheritdoc/>
    public override string Symbol => "{><}";

    /// <summary><inheritdoc/></summary>
    /// <remarks>
    /// Two rules that between them say the intervals are not disjoint — neither ends before the other begins.
    /// Non-strict on both, so bands that merely touch overlap. The containment ladder's <c>Overlaps</c> rung.
    /// </remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
    [
        new(Landmark.UpperBound, MustBe.GreaterThanOrEqualTo, Landmark.LowerBound),
        new(Landmark.LowerBound, MustBe.LessThanOrEqualTo, Landmark.UpperBound),
    ];
}
