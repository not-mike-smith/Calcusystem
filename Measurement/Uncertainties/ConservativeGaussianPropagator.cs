using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Measurement.Uncertainties;

/// <summary>
/// The default <see cref="IUncertaintyPropagator"/>: combines operands by root-sum-of-squares when they are
/// uncorrelated and by direct sum when they are correlated.
/// </summary>
/// <remarks>
/// Conservative in two senses. It takes the larger of an asymmetric operand's two magnitudes when it needs one
/// number, and it preserves asymmetry rather than averaging it away: all-symmetric operands give a
/// <see cref="SymmetricUncertainty"/>, and any asymmetric operand gives an <see cref="AsymmetricUncertainty"/>.
/// </remarks>
public class ConservativeGaussianPropagator : IUncertaintyPropagator
{
    /// <summary>The shared instance. The propagator holds no state, so one serves every caller.</summary>
    public static ConservativeGaussianPropagator Instance { get; } = new ConservativeGaussianPropagator();

    /// <inheritdoc/>
    /// <remarks>
    /// Sums combine <i>absolute</i> magnitudes, and the result stores one. Relative magnitudes would need
    /// dividing by the sum, which is not defined when the addends cancel to zero.
    /// </remarks>
    public IUncertainty PropagateThroughSum(
        UncertaintyCorrelation method,
        params Measurand[] measurands)
    {
        if (measurands.All(m => m.Uncertainty is ISymmetricUncertainty))
        {
            return PropagateSymmetricThroughSum(method, measurands);
        }

        double upperAbsoluteUncertainty = method switch
        {
            UncertaintyCorrelation.Uncorrelated => measurands.RootSumOfSquares(m => m.KmsUpperAbsoluteUncertainty),
            UncertaintyCorrelation.Correlated => measurands.Sum(m => m.KmsUpperAbsoluteUncertainty),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        double lowerAbsoluteUncertainty = method switch
        {
            UncertaintyCorrelation.Uncorrelated => measurands.RootSumOfSquares(m => m.KmsLowerAbsoluteUncertainty),
            UncertaintyCorrelation.Correlated => measurands.Sum(m => m.KmsLowerAbsoluteUncertainty),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        return AsymmetricUncertainty.From(true, upperAbsoluteUncertainty, lowerAbsoluteUncertainty);
    }

    private IUncertainty PropagateSymmetricThroughSum(
        UncertaintyCorrelation method,
        params Measurand[] measurands)
    {
        double absoluteUncertainty = method switch
        {
            UncertaintyCorrelation.Uncorrelated => measurands.RootSumOfSquares(m => m.KmsAbsoluteUncertainty),
            UncertaintyCorrelation.Correlated => measurands.Sum(m => m.KmsAbsoluteUncertainty),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        return SymmetricUncertainty.FromKmsAbsErr(absoluteUncertainty);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Products combine <i>relative</i> magnitudes, and the result stores one.
    /// </remarks>
    public IUncertainty PropagateThroughProduct(
        UncertaintyCorrelation method,
        params Measurand[] measurands)
    {
        if (measurands.All(m => m.Uncertainty is ISymmetricUncertainty))
        {
            return PropagateSymmetricThroughProduct(method, measurands);
        }

        double upperRelativeUncertainty = method switch
        {
            UncertaintyCorrelation.Uncorrelated => measurands.RootSumOfSquares(m => m.UpperRelativeUncertainty),
            UncertaintyCorrelation.Correlated => measurands.Sum(m => m.UpperRelativeUncertainty),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        double lowerRelativeUncertainty = method switch
        {
            UncertaintyCorrelation.Uncorrelated => measurands.RootSumOfSquares(m => m.LowerRelativeUncertainty),
            UncertaintyCorrelation.Correlated => measurands.Sum(m => m.LowerRelativeUncertainty),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        return AsymmetricUncertainty.From(false, upperRelativeUncertainty, lowerRelativeUncertainty);
    }

    private IUncertainty PropagateSymmetricThroughProduct(
        UncertaintyCorrelation method,
        params Measurand[] measurands)
    {
        var relErr = method switch
        {
            UncertaintyCorrelation.Uncorrelated => measurands.RootSumOfSquares(m => m.RelativeUncertainty),
            UncertaintyCorrelation.Correlated => measurands.Sum(m => m.RelativeUncertainty),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        return SymmetricUncertainty.FromRelative(relErr);
    }
}