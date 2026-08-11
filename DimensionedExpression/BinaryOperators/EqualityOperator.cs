using Calcusystem.DimensionedExpression.Interfaces;

using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.BaseModels;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the Lhs and Rhs are judged equal by the injected <see cref="IEqualityEstimating"/> strategy,
/// which decides what "equal enough" means given each side's uncertainty (exact equality is rarely meaningful
/// for measured values).
/// <br/>
/// Symbol: <b>==</b>
/// <br/>
/// Use when two quantities are expected to be the same and you want a pluggable notion of equality rather than
/// a fixed tolerance rule.
/// <br/>
/// The only operator that can be <see cref="IsDetermining"/>: an equality is the one relation from which a
/// solver can derive a value. Whether a given instance is an equation to solve or an assertion to check is the
/// modeller's call, so <paramref name="isDetermining"/> has no default — every construction states its intent.
/// </summary>
/// <param name="equalityEstimator">Decides what "equal enough" means given each side's uncertainty.</param>
/// <param name="isDetermining">
/// <see langword="true"/> when this equality is an equation the solver may use to compute an unknown (e.g.
/// <c>mass_in == mass_out</c>); <see langword="false"/> when it asserts a check over values that are already
/// determined (e.g. <c>measured_T == design_T</c>).
/// </param>
public class EqualityOperator(IEqualityEstimating equalityEstimator, bool isDetermining)
    : CommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.Equality;

    public override string Symbol => "==";

    /// <inheritdoc/>
    public override bool IsDetermining { get; } = isDetermining;

    public override bool? IsSatisfied()
    {
        if (Lhs.IsFullyDescribed is false || Rhs.IsFullyDescribed is false)
            return null;

        return equalityEstimator.AreEqual(Lhs.Value!, Rhs.Value!);
    }
}
