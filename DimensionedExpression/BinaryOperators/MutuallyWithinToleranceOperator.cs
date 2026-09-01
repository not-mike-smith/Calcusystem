
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when each side's nominal value falls within the other side's tolerance band — i.e.
/// Lhs ∈ [Rhs ± Rhs.error] AND Rhs ∈ [Lhs ± Lhs.error]. The check is symmetric.
/// <br/>
/// Symbol: <b>≃</b>
/// <br/>
/// Use when two independently measured quantities are expected to agree within their own stated uncertainties.
/// </summary>
public class MutuallyWithinToleranceOperator : CommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.MutuallyWithinTolerance;

    public override string Symbol => "≃";

    /// <inheritdoc/>
    /// <remarks>
    /// Nominal containment stated in both directions, and written out rather than derived by mirroring: because
    /// a rule names the landmark on each side independently, "the band's reported value lies between my bounds"
    /// is directly expressible. Deliberately <i>not</i> a rung — the containment ladder runs one way, and this
    /// is a quantifier over it.
    /// </remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
    [
        new(Landmark.Nominal, ComparisonType.GreaterThanOrEqualTo, Landmark.LowerBound),
        new(Landmark.Nominal, ComparisonType.LessThanOrEqualTo, Landmark.UpperBound),
        new(Landmark.LowerBound, ComparisonType.LessThanOrEqualTo, Landmark.Nominal),
        new(Landmark.UpperBound, ComparisonType.GreaterThanOrEqualTo, Landmark.Nominal),
    ];
}
