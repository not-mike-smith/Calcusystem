using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement.BaseClasses;
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

    public PrecisionQuantity? Value => IsFullyDescribed
        ? PrecisionQuantity.Product(ErrorPropagation, Factors.Select(f => f.Value!).ToArray())
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
        throw new NotImplementedException();
    }
}
