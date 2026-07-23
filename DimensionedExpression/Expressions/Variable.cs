using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement;
using Measurement.Exceptions;
using Measurement.Models;

namespace DimensionedExpression.Expressions;

public class Variable : IdBase, IDirectExpression
{
    // ReSharper disable once InconsistentNaming
    protected Measurand? _value;
    // ReSharper disable once InconsistentNaming
    protected string _symbol;

    public Variable(
        string symbol,
        Dimensionality dimensionality,
        string id = Constants.CREATE_NEW)
        : base(id)
    {
        
        Dimensionality = dimensionality;
        _symbol = symbol;
    }

    public Variable(
        string symbol,
        Measurand measurand,
        string id = Constants.CREATE_NEW)
        : base(id)
    {
        Dimensionality = measurand.Dimensionality;
        _value = measurand;
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
                throw new IncompatibleDimensionsException("Measurand must match dimensionality of Expression");

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
