using DimensionedExpression.Interfaces;
using Measurement.BaseClasses;
using Measurement.Models;

namespace DimensionedExpression.BaseModels;

public abstract class DirectExpressionBase : IdBase, IDirectExpression
{
    // ReSharper disable once InconsistentNaming
    protected PrecisionQuantity? _value;

    protected DirectExpressionBase(Dimensionality dimensionality, string id)
        : base(id)
    {
        Dimensionality = dimensionality;
    }

    protected DirectExpressionBase(PrecisionQuantity quantity, string id)
        : base(id)
    {
        Dimensionality = quantity.Dimensionality;
        _value = quantity;
    }

    public bool IsDirectlyMutable => true;
    public bool IsFullyDescribed => Value != null;
    public Dimensionality Dimensionality { get; }
    public abstract PrecisionQuantity? Value { get; set; }

    public int DegreesOfFreedom()
    {
        return IsFullyDescribed ? 0 : 1;
    }
}
