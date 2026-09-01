
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the Lhs and Rhs tolerance bands overlap at all — i.e. there exists at least one value
/// that is consistent with both uncertainties. This is the weakest form of agreement: even a single
/// shared point in the two intervals is sufficient.
/// <br/>
/// Symbol: <b>{><}</b>
/// <br/>
/// Use when checking whether two measurements are at least plausibly compatible, without requiring
/// that one falls squarely within the other's band.
/// </summary>
public class AnyToleranceOverlapOperator : CommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.AnyToleranceOverlap;

    /// <inheritdoc/>
    /// <remarks>
    /// The bands crossing: each interval reaches past where the other begins. <c>≈</c> used to sit here and
    /// oversold the claim — two measurements with wildly different reported values overlap freely once their
    /// error bars are fat enough, which is nothing like "approximately equal". <c>{&lt;&gt;}</c> would be the
    /// obvious spelling for disjoint, should it ever be wanted.
    /// </remarks>
    public override string Symbol => "{><}";

    /// <inheritdoc/>
    /// <remarks>
    /// Two rules that between them say the intervals are not disjoint — neither ends before the other begins.
    /// Non-strict on both, so bands that merely touch overlap. Commutative, and visibly so: swapping the
    /// operands maps each rule onto the other. The containment ladder's <c>Overlaps</c> rung.
    /// </remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
    [
        new(Landmark.UpperBound, ComparisonType.GreaterThanOrEqualTo, Landmark.LowerBound),
        new(Landmark.LowerBound, ComparisonType.LessThanOrEqualTo, Landmark.UpperBound),
    ];
}
