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
/// The only operator whose <see cref="SolvingRole"/> can be anything but a requirement: an equality is the one
/// relation from which a solver can derive a value. Which of the three a given instance is remains the
/// modeller's call, so <paramref name="solvingRole"/> has no default — every construction states its intent.
/// </summary>
/// <param name="equalityEstimator">Decides what "equal enough" means given each side's uncertainty.</param>
/// <param name="solvingRole">
/// <see cref="DimensionedExpression.SolvingRole.Equation"/> when this defines a quantity the solver may compute
/// (<c>mass_in == mass_out</c>); <see cref="DimensionedExpression.SolvingRole.Coherence"/> when it asserts that
/// two independently computed routes to one quantity agree (<c>T_eos == T_path</c>);
/// <see cref="DimensionedExpression.SolvingRole.Requirement"/> when it checks a value against a criterion
/// (<c>measured_T == design_T</c>).
/// </param>
public class EqualityOperator(IEqualityEstimating equalityEstimator, SolvingRole solvingRole)
    : CommutativeOperatorBase
{
    protected override BinaryOperatorKind Kind => BinaryOperatorKind.Equality;

    public override string Symbol => "==";

    /// <inheritdoc/>
    public override SolvingRole SolvingRole { get; } = solvingRole;

    public override bool? IsSatisfied()
    {
        // One walk per side. `ComputeIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.ComputeIfDetermined();
        var rhs = Rhs.ComputeIfDetermined();
        if (lhs is null || rhs is null) return null;

        return equalityEstimator.AreEqual(lhs, rhs);
    }
}
