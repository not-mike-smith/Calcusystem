using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the entire Lhs uncertainty interval [Lhs ± Lhs.uncertainty] is strictly contained within
/// the Rhs tolerance band [Rhs ± Rhs.uncertainty]. Both the lower and upper bounds of Lhs must lie inside
/// the Rhs interval; the Lhs interval touching the Rhs boundary does not satisfy this operator.
/// <br/><br/>
/// Symbol: <b>[=}</b>
/// </summary>
/// <remarks>
/// The only containment operator with a bracket rather than a dot on the left, and the only strict one. Its
/// siblings place a <i>point</i> in a closed band; this places an <i>interval</i> inside an open one, which is a
/// different claim — an interval is not strictly inside a copy of itself.
/// </remarks>
public class WhollyWithinToleranceOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorType Type => BinaryOperatorType.WhollyWithinTolerance;

    public override string Symbol => "[=}";

    /// <summary><inheritdoc/></summary>
    /// <remarks>
    /// The containment ladder's <c>WhollyWithin</c> rung. Strict on both bounds, which is what separates it from
    /// the rest of the family — see the class summary.
    /// </remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
    [
        new(Landmark.LowerBound, MustBe.GreaterThan, Landmark.LowerBound),
        new(Landmark.UpperBound, MustBe.LessThan, Landmark.UpperBound),
    ];
}
