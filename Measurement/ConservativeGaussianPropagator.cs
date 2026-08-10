using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Extensions;

namespace Calcusystem.Measurement;

public class ConservativeGaussianPropagator : IErrorPropagator
{
    public static ConservativeGaussianPropagator Instance { get; } = new ConservativeGaussianPropagator();

    public IUncertainty PropagateErrorThroughSum(
        ErrorPropagationMethod method,
        params Measurand[] measurands)
    {
        if (measurands.All(m => m.Uncertainty is ISymmetricUncertainty))
        {
            return PropagateSymmetricErrorThroughSum(method, measurands);
        }

        double upperAbsoluteError = method switch
        {
            ErrorPropagationMethod.Uncorrelated => measurands.RootSumOfSquares(m => m.KmsUpperAbsoluteError),
            ErrorPropagationMethod.Correlated => measurands.Sum(m => m.KmsUpperAbsoluteError),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        double lowerAbsoluteError = method switch
        {
            ErrorPropagationMethod.Uncorrelated => measurands.RootSumOfSquares(m => m.KmsLowerAbsoluteError),
            ErrorPropagationMethod.Correlated => measurands.Sum(m => m.KmsLowerAbsoluteError),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        return AsymmetricUncertainty.From(true, upperAbsoluteError, lowerAbsoluteError);
    }

    private IUncertainty PropagateSymmetricErrorThroughSum(
        ErrorPropagationMethod method,
        params Measurand[] measurands)
    {
        double absoluteError = method switch
        {
            ErrorPropagationMethod.Uncorrelated => measurands.RootSumOfSquares(m => m.KmsAbsoluteError),
            ErrorPropagationMethod.Correlated => measurands.Sum(m => m.KmsAbsoluteError),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        // Store the propagated error as an absolute value rather than dividing by the (possibly zero) sum —
        // this is what keeps a sum that cancels to zero well-defined.
        return SymmetricUncertainty.FromKmsAbsErr(absoluteError);
    }

    public IUncertainty PropagateErrorThroughProduct(
        ErrorPropagationMethod method,
        params Measurand[] measurands)
    {
        if (measurands.All(m => m.Uncertainty is ISymmetricUncertainty))
        {
            return PropagateSymmetricErrorThroughProduct(method, measurands);
        }

        double upperRelativeError = method switch
        {
            ErrorPropagationMethod.Uncorrelated => measurands.RootSumOfSquares(m => m.UpperRelativeError),
            ErrorPropagationMethod.Correlated => measurands.Sum(m => m.UpperRelativeError),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        double lowerRelativeError = method switch
        {
            ErrorPropagationMethod.Uncorrelated => measurands.RootSumOfSquares(m => m.LowerRelativeError),
            ErrorPropagationMethod.Correlated => measurands.Sum(m => m.LowerRelativeError),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        return AsymmetricUncertainty.From(false, upperRelativeError, lowerRelativeError);
    }

    private IUncertainty PropagateSymmetricErrorThroughProduct(
        ErrorPropagationMethod method,
        params Measurand[] measurands)
    {
        var relErr = method switch
        {
            ErrorPropagationMethod.Uncorrelated => measurands.RootSumOfSquares(m => m.RelativeError),
            ErrorPropagationMethod.Correlated => measurands.Sum(m => m.RelativeError),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        return SymmetricUncertainty.FromRelErr(relErr);
    }
}