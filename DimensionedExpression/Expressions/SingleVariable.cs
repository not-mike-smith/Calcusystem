using DimensionedExpression.BaseModels;
using Measurement.BaseClasses;
using Measurement.Exceptions;
using Measurement.Models;

namespace DimensionedExpression.Expressions;

public class SingleVariable : DirectExpressionBase
{
    private string _symbol;

    public SingleVariable(
        string symbol,
        Dimensionality dimensionality,
        string id = Constants.CREATE_NEW) : base(dimensionality, id)
    {
        _symbol = symbol;
    }

    public SingleVariable(
        string symbol,
        PrecisionQuantity quantity,
        string id = Constants.CREATE_NEW) : base(quantity, id)
    {
        _symbol = symbol;
    }

    public override PrecisionQuantity? Value
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
