using DimensionedExpression.Interfaces;
using Measurement.BaseClasses;
using Measurement.Exceptions;
using Measurement.Models;

namespace DimensionedExpression.BaseModels;

public abstract class DirectExpressionBase<T> : IdBase, IDirectExpression<T> where T : PrecisionQuantity
{
    // ReSharper disable once InconsistentNaming
    protected PrecisionQuantity? _value;
    // ReSharper disable once InconsistentNaming
    protected string _symbol;

    protected DirectExpressionBase(string symbol, Dimensionality dimensionality, string id)
        : base(id)
    {
        Dimensionality = dimensionality;
        _symbol = symbol;
    }

    protected DirectExpressionBase(string symbol, PrecisionQuantity quantity, string id)
        : base(id)
    {
        Dimensionality = quantity.Dimensionality;
        _value = quantity;
        _symbol = symbol;
    }

    public bool IsDirectlyMutable => true;
    public bool IsFullyDescribed => Value != null;
    public Dimensionality Dimensionality { get; }
    public int DegreesOfFreedom()
    {
        return IsFullyDescribed ? 0 : 1;
    }

    public T? Value
    {
        get => (T?)_value;
        set
        {
            if (value != null && value.Dimensionality != Dimensionality)
                throw new IncompatibleDimensionsException("Quantity must match dimensionality of SingleVariable");

            _value = value;
        }
    }

    PrecisionQuantity? IExpression.Value => Value;

    public string Symbol
    {
        get => _symbol;
        set => _symbol = value;
    }

    public override string ToString()
    {
        return Symbol;
    }
}
