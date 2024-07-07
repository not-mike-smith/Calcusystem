using Measurement;
using Measurement.BaseClasses;
using Measurement.Models;

namespace DimensionedExpression.Interfaces;

public interface IExpression
{
    public string Id { get; }
    bool IsDirectlyMutable { get; }
    bool IsFullyDescribed { get; }
    Dimensionality Dimensionality { get; }
    PrecisionQuantity? Value { get; }
    int DegreesOfFreedom(); // TODO is this realistic?
}

public interface ICalculatedExpression : IExpression
{
    ErrorPropagationMethod ErrorPropagation { get; set; }
}

public interface IDirectExpression<T> : IExpression where T : PrecisionQuantity
{
    new T? Value { get; set; }
}
