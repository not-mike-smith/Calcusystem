using DimensionedExpression.Interfaces;
using Measurement;
using Measurement.Exceptions;
using Measurement.Models;

namespace DimensionedExpression.BaseModels;

public abstract class DirectExpressionBase : IdBase, IDirectExpression
{
    // ReSharper disable once InconsistentNaming
    protected Measurand? _value;
    // ReSharper disable once InconsistentNaming
    protected string _symbol;

    protected DirectExpressionBase(string symbol, Dimensionality dimensionality, string id)
        : base(id)
    {
        Dimensionality = dimensionality;
        _symbol = symbol;
    }

    protected DirectExpressionBase(string symbol, Measurand quantity, string id)
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

    public Measurand? Value
    {
        get => _value;
        set
        {
            if (value != null && value.Dimensionality != Dimensionality)
                throw new IncompatibleDimensionsException("Quantity must match dimensionality of SingleVariable");

            _value = value;
        }
    }

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
