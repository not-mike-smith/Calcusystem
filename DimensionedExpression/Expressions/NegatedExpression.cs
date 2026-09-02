using Calcusystem.Core.Identity;
using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Unary negation of any <see cref="IExpression"/> (its <see cref="Operand"/>): the same dimensionality, with
/// the operand's value and uncertainty negated.
/// <br/>
/// Not directly mutable; <see cref="Value"/> is null until the operand is fully described.
/// </summary>
public class NegatedExpression : ExpressionBase, IExpression, ISnapshottingNode<NegatedExpression, UnaryExpressionSnapshot>
{
    public NegatedExpression(IExpression operand, string id = Constants.CREATE_NEW_ID) : base(id)
    {
        _operand = operand;
    }

    private readonly IExpression _operand;

    public IExpression Operand => _operand;

    public override bool IsDirectlyMutable => false;
    public override bool IsFullyDescribed => Operand.IsFullyDescribed;
    public override Dimensionality Dimensionality => Operand.Dimensionality;

    /// <inheritdoc/>
    public override Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IUncertaintyPropagator? propagator = null) =>
        known.TryGetValue(Operand, out var operand) ? -operand : null;

    /// <inheritdoc/>
    public override IEnumerable<IExpression> Children => [Operand];

    public override string ToString()
    {
        return $"-{Operand}";
    }

    /// <inheritdoc/>
    public UnaryExpressionSnapshot GetSnapshot() =>
        new(UnaryExpressionType.Negated, Id, Operand.Id);

    /// <inheritdoc/>
    public static NegatedExpression FromSnapshot(UnaryExpressionSnapshot state, INodeResolver resolve) =>
        new(resolve.Resolve<IExpression>(state.InnerId), state.Id);
}
