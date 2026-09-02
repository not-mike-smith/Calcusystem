using Calcusystem.Core.Identity;
using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Unary square root of any <see cref="IExpression"/>. The result's dimensionality is the argument's with every
/// exponent halved (e.g. √(m²·s⁻²) → m·s⁻¹), so every exponent must be even — an odd exponent throws
/// <see cref="Measurement.Exceptions.NondiscreteDimensionalityException"/>. A negative argument value yields a
/// NaN result.
/// <br/>
/// Uncertainty follows the power rule: RelativeError(√x) = ½·RelativeError(x).
/// </summary>
public class SqrtExpression : ExpressionBase, IExpression, IStatefulNode<SqrtExpression, UnaryExpressionState>
{
    private readonly IExpression _argument;

    public SqrtExpression(IExpression argument, string id = Constants.CREATE_NEW_ID) : base(id)
    {
        _argument = argument;
    }

    public IExpression Argument => _argument;

    public override bool IsDirectlyMutable => false;
    public override bool IsFullyDescribed => Argument.IsFullyDescribed;

    // Each exponent halved; throws NondiscreteDimensionalityException if any argument exponent is odd.
    public override Dimensionality Dimensionality => Argument.Dimensionality / 2;

    /// <inheritdoc/>
    public override Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IErrorPropagator? propagator = null) =>
        known.TryGetValue(Argument, out var operand) ? operand.ToRoot(2) : null;

    public override string ToString()
    {
        return $"√({Argument})";
    }

    /// <inheritdoc/>
    public override IEnumerable<IExpression> Children => [Argument];

    /// <inheritdoc/>
    public UnaryExpressionState GetState() =>
        new(UnaryExpressionKind.Sqrt, Id, Argument.Id);

    /// <inheritdoc/>
    public static SqrtExpression FromState(UnaryExpressionState state, INodeResolver resolve) =>
        new(resolve.Resolve<IExpression>(state.InnerId), state.Id);
}
