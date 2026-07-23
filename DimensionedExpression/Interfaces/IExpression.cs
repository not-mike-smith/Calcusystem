using Measurement;
using Measurement.Models;

namespace DimensionedExpression.Interfaces;

public interface IExpression
{
    public string Id { get; }
    bool IsDirectlyMutable { get; }
    bool IsFullyDescribed { get; }
    Dimensionality Dimensionality { get; }
    Measurand? Value { get; }
    int DegreesOfFreedom(); // TODO is this realistic?
}

public interface ICalculatedExpression : IExpression
{
    ErrorPropagationMethod ErrorPropagation { get; set; }
}

public interface IDirectExpression : IExpression
{
    new Measurand? Value { get; set; }
}
