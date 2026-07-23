using DimensionedExpression.BaseModels;
using Measurement;
using Measurement.Models;

namespace DimensionedExpression.Expressions;

public class Variable : DirectExpressionBase
{
    public Variable(
        string symbol,
        Dimensionality dimensionality,
        string id = Constants.CREATE_NEW) : base(symbol, dimensionality, id)
    { }

    public Variable(
        string symbol,
        Measurand quantity,
        string id = Constants.CREATE_NEW) : base(symbol, quantity, id)
    { }
}
