using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement;
using Measurement.Models;

namespace DimensionedExpression.Expressions;

public class ProductExpression : CalculatedExpressionBase, ICalculatedExpression
{
    private readonly List<IExpression> _factors = new();

    public IReadOnlyList<IExpression> Factors => _factors;
    public bool IsFullyDescribed => Factors.All(f => f.IsFullyDescribed);

    public Dimensionality Dimensionality => Factors.Aggregate(
        Dimensionality.Dimensionless,
        (productDimensions, current) => productDimensions * current.Dimensionality);

    public Measurand? Value => IsFullyDescribed && _factors.Count > 0
        ? _factors.Select(f => f.Value!).Skip(1)
            .Aggregate(_factors[0].Value!, (acc, f) => acc.Times(f, ErrorPropagation))
        : null;

    public void AddFactor(IExpression expression)
    {
        _factors.Add(expression);
    }

    public bool RemoveFactor(IExpression expression)
    {
        return _factors.Remove(expression);
    }

    public override string ToString()
    {
        return $"({string.Join('·', Factors.Select(f => f.ToString()))})";
    }

    public int DegreesOfFreedom()
    {
        return _factors.Sum(f => f.DegreesOfFreedom());
    }
}
