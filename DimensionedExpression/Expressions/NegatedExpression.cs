using Calcusystem.DimensionedExpression.State;
using Calcusystem.Core;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Unary negation of any <see cref="IExpression"/> (its <see cref="Operand"/>): the same dimensionality, with
/// the operand's value and uncertainty negated.
/// <br/>
/// Not directly mutable; <see cref="Value"/> is null until the operand is fully described.
/// </summary>
public class NegatedExpression : IdBase, IExpression, IStatefulNode<NegatedExpression, UnaryExpressionState>
{
    public NegatedExpression(IExpression operand, string id = Constants.CREATE_NEW_ID) : base(id)
    {
        _operand = operand;
    }

    private IExpression _operand;

    public IExpression Operand
    {
        get => _operand;
        set => _operand = value;
    }

    public bool IsDirectlyMutable => false;
    public bool IsFullyDescribed => Operand.IsFullyDescribed;
    public Dimensionality Dimensionality => Operand.Dimensionality;
    public Measurand? Value => Operand.IsFullyDescribed ? -(Operand.Value!) : null;

    /// <inheritdoc/>
    public IEnumerable<IExpression> Children => [Operand];

    public override string ToString()
    {
        return $"-{Operand}";
    }

    /// <inheritdoc/>
    public UnaryExpressionState GetState() =>
        new(UnaryExpressionKind.Negated, Id, Operand.Id);

    /// <inheritdoc/>
    public static NegatedExpression FromState(UnaryExpressionState state, INodeResolver resolve) =>
        new(resolve.Resolve<IExpression>(state.InnerId), state.Id);
}
