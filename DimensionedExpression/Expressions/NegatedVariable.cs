using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement.BaseClasses;
using Measurement.Models;

namespace DimensionedExpression.Expressions;

public class NegatedVariable : IdBase, IExpression
{
    public NegatedVariable(string id = Constants.CREATE_NEW) : base(id)
    {
    }

    private IExpression _negatedExpression;

    public IExpression NegatedExpression
    {
        get => _negatedExpression;
        set => _negatedExpression = value;
    }

    public bool IsDirectlyMutable => false;
    public bool IsFullyDescribed => NegatedExpression.IsFullyDescribed;
    public Dimensionality Dimensionality => NegatedExpression.Dimensionality;
    public PrecisionQuantity? Value => NegatedExpression.IsFullyDescribed ? -(NegatedExpression.Value!) : null;

    public int DegreesOfFreedom()
    {
        return NegatedExpression.DegreesOfFreedom();
    }

    public override string ToString()
    {
        return $"-{NegatedExpression}";
    }
}
