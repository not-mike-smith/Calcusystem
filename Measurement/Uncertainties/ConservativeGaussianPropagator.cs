using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Measurement.Uncertainties;

public class ConservativeGaussianPropagator : IUncertaintyPropagator
{
    public static ConservativeGaussianPropagator Instance { get; } = new ConservativeGaussianPropagator();

    public IUncertainty PropagateErrorThroughSum(
        UncertaintyPropagation method,
        params Measurand[] measurands)
    {
        if (measurands.All(m => m.Uncertainty is ISymmetricUncertainty))
        {
            return PropagateSymmetricErrorThroughSum(method, measurands);
        }

        double upperAbsoluteUncertainty = method switch
        {
            UncertaintyPropagation.Uncorrelated => measurands.RootSumOfSquares(m => m.KmsUpperAbsoluteUncertainty),
            UncertaintyPropagation.Correlated => measurands.Sum(m => m.KmsUpperAbsoluteUncertainty),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        double lowerAbsoluteUncertainty = method switch
        {
            UncertaintyPropagation.Uncorrelated => measurands.RootSumOfSquares(m => m.KmsLowerAbsoluteUncertainty),
            UncertaintyPropagation.Correlated => measurands.Sum(m => m.KmsLowerAbsoluteUncertainty),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        return AsymmetricUncertainty.From(true, upperAbsoluteUncertainty, lowerAbsoluteUncertainty);
    }

    private IUncertainty PropagateSymmetricErrorThroughSum(
        UncertaintyPropagation method,
        params Measurand[] measurands)
    {
        double absoluteUncertainty = method switch
        {
            UncertaintyPropagation.Uncorrelated => measurands.RootSumOfSquares(m => m.KmsAbsoluteUncertainty),
            UncertaintyPropagation.Correlated => measurands.Sum(m => m.KmsAbsoluteUncertainty),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        // Store the propagated error as an absolute value rather than dividing by the (possibly zero) sum —
        // this is what keeps a sum that cancels to zero well-defined.
        return SymmetricUncertainty.FromKmsAbsErr(absoluteUncertainty);
    }

    public IUncertainty PropagateErrorThroughProduct(
        UncertaintyPropagation method,
        params Measurand[] measurands)
    {
        if (measurands.All(m => m.Uncertainty is ISymmetricUncertainty))
        {
            return PropagateSymmetricErrorThroughProduct(method, measurands);
        }

        double upperRelativeUncertainty = method switch
        {
            UncertaintyPropagation.Uncorrelated => measurands.RootSumOfSquares(m => m.UpperRelativeUncertainty),
            UncertaintyPropagation.Correlated => measurands.Sum(m => m.UpperRelativeUncertainty),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        double lowerRelativeUncertainty = method switch
        {
            UncertaintyPropagation.Uncorrelated => measurands.RootSumOfSquares(m => m.LowerRelativeUncertainty),
            UncertaintyPropagation.Correlated => measurands.Sum(m => m.LowerRelativeUncertainty),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        return AsymmetricUncertainty.From(false, upperRelativeUncertainty, lowerRelativeUncertainty);
    }

    private IUncertainty PropagateSymmetricErrorThroughProduct(
        UncertaintyPropagation method,
        params Measurand[] measurands)
    {
        var relErr = method switch
        {
            UncertaintyPropagation.Uncorrelated => measurands.RootSumOfSquares(m => m.RelativeUncertainty),
            UncertaintyPropagation.Correlated => measurands.Sum(m => m.RelativeUncertainty),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        return SymmetricUncertainty.FromRelative(relErr);
    }
}