using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement.BaseClasses;
using Measurement.Models;

namespace DimensionedExpression.Expressions;

public class ReciprocalExpression : IdBase, IExpression
{
    private IExpression _reciprocand;

    public ReciprocalExpression(IExpression reciprocand, string id = Constants.CREATE_NEW) : base(id)
    {
        _reciprocand = reciprocand;
    }

    public IExpression Reciprocand
    {
        get => _reciprocand;
        set => _reciprocand = value;
    }

    public bool IsDirectlyMutable => false;
    public bool IsFullyDescribed => Reciprocand.IsFullyDescribed;
    public Dimensionality Dimensionality => Reciprocand.Dimensionality.Reciprocal();

    public PrecisionQuantity? Value => IsFullyDescribed
        ? Reciprocand.Value!.Reciprocal()
        : null;

    public override string ToString()
    {
        return $"1/({Reciprocand})";
    }

    public int DegreesOfFreedom()
    {
        return Reciprocand.DegreesOfFreedom();
    }

}
