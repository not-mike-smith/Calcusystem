using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Measurement.Interfaces;

/// <summary>
/// Strategy for propagating measurement uncertainty through arithmetic on <see cref="Measurand"/>s.
/// Each method takes the operands and returns the uncertainty of the result. Implementations decide
/// how errors combine (e.g. root-sum-of-squares vs. direct sum) and what concrete
/// <see cref="IUncertainty"/> type the result carries.
/// </summary>
/// <remarks>
/// The propagator is the injection seam for alternative uncertainty models (Monte Carlo, correlation-aware,
/// etc.). The only implementation today is <c>ConservativeGaussianPropagator</c>. It combines the operands'
/// conservative errors, returning a symmetric result when all operands are symmetric and an asymmetric one
/// otherwise. (Unary transforms — negation, reciprocal, exponentiation — are not here; they live on
/// <see cref="IUncertainty"/> since they act on a single uncertainty.)
/// </remarks>
public interface IUncertaintyPropagator
{
    /// <summary>
    /// Propagates uncertainty through the product (and quotient, via reciprocal operands) of the operands.
    /// </summary>
    /// <param name="method">Whether the operands' errors are treated as correlated or uncorrelated.</param>
    /// <param name="measurands">The factors whose product's uncertainty is being computed.</param>
    /// <returns>The uncertainty of the product.</returns>
    IUncertainty PropagateThroughProduct(UncertaintyCorrelation method, params Measurand[] measurands);

    /// <summary>
    /// Propagates uncertainty through the sum (and difference, via negated operands) of the operands.
    /// </summary>
    /// <param name="method">Whether the operands' errors are treated as correlated or uncorrelated.</param>
    /// <param name="measurands">The addends whose sum's uncertainty is being computed.</param>
    /// <returns>The uncertainty of the sum.</returns>
    IUncertainty PropagateThroughSum(UncertaintyCorrelation method, params Measurand[] measurands);
}
