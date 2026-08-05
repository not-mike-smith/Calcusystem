using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement;

namespace DimensionedExpression.Expressions;

/// <summary>
/// Unary reciprocal (<c>1/x</c>) of any <see cref="IExpression"/> (its <see cref="Reciprocand"/>); the result
/// dimensionality is the reciprocand's inverted (e.g. t → t⁻¹).
/// <br/>
/// Not directly mutable; <see cref="Value"/> is null until the reciprocand is fully described.
/// </summary>
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

    public Measurand? Value => IsFullyDescribed
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
