using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Measurement.Uncertainties;

public class ConservativeGaussianPropagator : IUncertaintyPropagator
{
    public static ConservativeGaussianPropagator Instance { get; } = new ConservativeGaussianPropagator();

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

        // Store the propagated error as an absolute value rather than dividing by the (possibly zero) sum —
        // this is what keeps a sum that cancels to zero well-defined.
        return SymmetricUncertainty.FromKmsAbsErr(absoluteUncertainty);
    }

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