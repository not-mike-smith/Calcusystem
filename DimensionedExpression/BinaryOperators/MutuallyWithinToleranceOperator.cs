
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
    /// Nominal containment stated in both directions. Because a rule names the landmark on each side
    /// independently, the reverse direction is written directly — the band's reported value against the
    /// subject's bounds — rather than by evaluating the operator a second time with the operands swapped.
    /// </remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } =
    [
        .. ContainmentLadder.NominalWithinRules,
        .. ContainmentLadder.NominalWithinRules.Select(rule => rule.Mirrored),
    ];
}
