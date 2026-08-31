
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the entire Lhs uncertainty interval [Lhs ± Lhs.error] is strictly contained within
/// the Rhs tolerance band [Rhs ± Rhs.error]. Both the lower and upper bounds of Lhs must lie inside
/// the Rhs interval; the Lhs interval touching the Rhs boundary does not satisfy this operator.
/// <br/>
/// Symbol: <b>[=}</b>
/// <br/>
/// Use for worst-case bilateral conformance checks where no part of the measurement's uncertainty range
/// may fall outside the specification.
/// </summary>
/// <remarks>
/// The only containment operator with a bracket rather than a dot on the left, and the only strict one. Its
/// siblings place a <i>point</i> in a closed band; this places an <i>interval</i> inside an open one, which is a
/// different claim — an interval is not strictly inside a copy of itself.
/// </remarks>
public class WhollyWithinToleranceOperator : NonCommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.WhollyWithinTolerance;

    public override string Symbol => "[=}";

    /// <inheritdoc/>
    public override IReadOnlyList<ComparisonRule> Rules { get; } = ContainmentLadder.WhollyWithinRules;
}
