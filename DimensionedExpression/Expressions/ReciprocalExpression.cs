using Calcusystem.Core.Identity;
using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Unary reciprocal (<c>1/x</c>) of any <see cref="IExpression"/> (its <see cref="Reciprocand"/>); the result
/// dimensionality is the reciprocand's inverted (e.g. t → t⁻¹).
/// <br/>
/// Not directly mutable; <see cref="Value"/> is null until the reciprocand is fully described.
/// </summary>
public class ReciprocalExpression : ExpressionBase, IExpression, IStatefulNode<ReciprocalExpression, UnaryExpressionState>
{
    private readonly IExpression _reciprocand;

    public ReciprocalExpression(IExpression reciprocand, string id = Constants.CREATE_NEW_ID) : base(id)
    {
        _reciprocand = reciprocand;
    }

    public IExpression Reciprocand => _reciprocand;

    public override bool IsDirectlyMutable => false;
    public override bool IsFullyDescribed => Reciprocand.IsFullyDescribed;
    public override Dimensionality Dimensionality => Reciprocand.Dimensionality.Reciprocal();

    /// <inheritdoc/>
    public override Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IErrorPropagator? propagator = null) =>
        known.TryGetValue(Reciprocand, out var operand) ? operand.Reciprocal() : null;

    public override string ToString()
    {
        return $"1/({Reciprocand})";
    }

    /// <inheritdoc/>
    public override IEnumerable<IExpression> Children => [Reciprocand];

    /// <inheritdoc/>
    public UnaryExpressionState GetState() =>
        new(UnaryExpressionKind.Reciprocal, Id, Reciprocand.Id);

    /// <inheritdoc/>
    public static ReciprocalExpression FromState(UnaryExpressionState state, INodeResolver resolve) =>
        new(resolve.Resolve<IExpression>(state.InnerId), state.Id);
}
