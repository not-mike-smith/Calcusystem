using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Core;
using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Interfaces;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// N-ary product (<c>×</c>) over its <see cref="Factors"/>; the result dimensionality is the product of the
/// factors' dimensionalities.
/// <br/>
/// A computed node: uncertainty is propagated through <see cref="Measurand"/> multiplication using the
/// <see cref="ComputedExpressionBase.ErrorPropagation"/> method, and <see cref="DegreesOfFreedom"/> is the sum
/// of the factors'.
/// </summary>
public class ProductExpression : ComputedExpressionBase, IComputedExpression, IStatefulNode<ProductExpression, NaryExpressionState>
{
    private readonly List<IExpression> _factors = new();

    public IReadOnlyList<IExpression> Factors => _factors;
    public bool IsFullyDescribed => Factors.All(f => f.IsFullyDescribed);

    public Dimensionality Dimensionality => Factors.Aggregate(
        Dimensionality.Dimensionless,
        (productDimensions, current) => productDimensions * current.Dimensionality);


    /// <inheritdoc/>
    /// <remarks>Factors are read in declaration order, so a factor listed twice contributes twice.</remarks>
    public Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IErrorPropagator? propagator = null) =>
        _factors.Count == 0
            ? null
            : _factors.Select(f => known[f]).Aggregate((acc, f) => acc.Times(f, ErrorPropagation, propagator));

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

    /// <inheritdoc/>
    public IEnumerable<IExpression> Children => _factors;

    /// <inheritdoc/>
    public NaryExpressionState GetState() =>
        new(NaryExpressionKind.Product, Id, Factors.Select(f => f.Id).ToList(), ErrorPropagation);

    /// <inheritdoc/>
    public static ProductExpression FromState(NaryExpressionState state, INodeResolver resolve)
    {
        var product = new ProductExpression { Id = state.Id, ErrorPropagation = state.ErrorPropagation };
        foreach (var id in state.InnerIds)
        {
            product.AddFactor(resolve.Resolve<IExpression>(id));
        }

        return product;
    }
}
