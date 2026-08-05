using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement;

namespace DimensionedExpression.Expressions;

/// <summary>
/// Unary negation of any <see cref="IExpression"/> (its <see cref="Operand"/>): the same dimensionality, with
/// the operand's value and uncertainty negated.
/// <br/>
/// Not directly mutable; <see cref="Value"/> is null until the operand is fully described.
/// </summary>
public class NegatedExpression : IdBase, IExpression
{
    public NegatedExpression(IExpression operand, string id = Constants.CREATE_NEW) : base(id)
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

    public int DegreesOfFreedom()
    {
        return Operand.DegreesOfFreedom();
    }

    public override string ToString()
    {
        return $"-{Operand}";
    }
}
