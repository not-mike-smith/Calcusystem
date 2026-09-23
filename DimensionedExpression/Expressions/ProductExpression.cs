using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// N-ary product (<c>×</c>) over its <see cref="Factors"/>; the result dimensionality is the product of the
/// factors' dimensionalities.
/// <br/><br/>
/// Uncertainty is propagated through <see cref="Measurand"/> multiplication under this
/// node's <see cref="ComputedExpressionBase.UncertaintyCorrelation"/>.
/// </summary>
public class ProductExpression : ComputedExpressionBase, IComputedExpression, ISnapshottingNode<ProductExpression, NaryExpressionSnapshot>
{
    private readonly List<IExpression> _factors;

    public ProductExpression(IEnumerable<IExpression> factors)
    {
        _factors = factors.ToList();
    }

    public IReadOnlyList<IExpression> Factors => _factors;
    public override bool IsFullyDescribed => Factors.All(f => f.IsFullyDescribed);

    public override Dimensionality Dimensionality => Factors.Aggregate(
        Dimensionality.Dimensionless,
        (productDimensions, current) => productDimensions * current.Dimensionality);

    /// <summary><inheritdoc/></summary>
    /// <remarks>Factors are read in declaration order, so a factor listed twice contributes twice.</remarks>
    public override Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IUncertaintyPropagator? propagator = null)
    {
        if (_factors.Count == 0 || _factors.Any(f => ! known.ContainsKey(f))) return null;

        // One n-ary call rather than folding pairwise: the propagator combines all the relative uncertainties at once
        // instead of building an intermediate Measurand per factor.
        return Measurand.Product(UncertaintyCorrelation, propagator, _factors.Select(f => known[f]).ToArray());
    }

    public override string ToString()
    {
        return $"({string.Join('·', Factors.Select(f => f.ToString()))})";
    }

    /// <inheritdoc/>
    public override IEnumerable<IExpression> Children => _factors;

    /// <inheritdoc/>
    public NaryExpressionSnapshot GetSnapshot() =>
        new(NaryExpressionType.Product, Id, Factors.Select(f => f.Id).ToList(), UncertaintyCorrelation);

    /// <inheritdoc/>
    public static ProductExpression FromSnapshot(NaryExpressionSnapshot snapshot, INodeResolver resolve) =>
        new(snapshot.InnerIds.Select(resolve.Resolve<IExpression>))
        {
            Id = snapshot.Id,
            UncertaintyCorrelation = snapshot.UncertaintyCorrelation,
        };
}
