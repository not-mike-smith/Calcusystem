using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement;
using Measurement.Models;

namespace DimensionedExpression.Expressions;

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
