using DimensionedExpression.BaseModels;
using Measurement;
using Measurement.BaseClasses;
using Measurement.Models;

namespace DimensionedExpression.Expressions;

public class DeltaVariable : DirectExpressionBase<Delta>
{
    public DeltaVariable(
        string symbol,
        Dimensionality dimensionality,
        string id = Constants.CREATE_NEW) : base(symbol, dimensionality, id)
    { }

    public DeltaVariable(
        string symbol,
        Delta quantity,
        string id = Constants.CREATE_NEW) : base(symbol, quantity, id)
    { }
}

public class MagnitudeVariable : DirectExpressionBase<Magnitude>
{
    public MagnitudeVariable(
        string symbol,
        Dimensionality dimensionality,
        string id = Constants.CREATE_NEW) : base(symbol, dimensionality, id)
    { }

    public MagnitudeVariable(
        string symbol,
        Magnitude quantity,
        string id = Constants.CREATE_NEW) : base(symbol, quantity, id)
    { }
}
