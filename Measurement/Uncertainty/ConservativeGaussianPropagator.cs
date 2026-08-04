using Measurement.Interfaces;

namespace Measurement.Uncertainty;

public class ConservativeGaussianPropagator : IErrorPropagator
{
    public static ConservativeGaussianPropagator Instance { get; } = new ConservativeGaussianPropagator();

    public IUncertainty PropagateErrorThroughSum(
        ErrorPropagationMethod method,
        params Measurand[] measurands)
    {
        double absoluteError = method switch
        {
            ErrorPropagationMethod.Uncorrelated => Math.Sqrt(measurands.Sum(m => m.KmsAbsoluteError * m.KmsAbsoluteError)),
            ErrorPropagationMethod.Correlated => measurands.Sum(m => m.KmsAbsoluteError),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        // Store the propagated error as an absolute value rather than dividing by the (possibly zero) sum —
        // this is what keeps a sum that cancels to zero well-defined.
        return GaussianUncertainty.FromKmsAbsErr(absoluteError);
    }

    public IUncertainty PropagateErrorThroughProduct(
        ErrorPropagationMethod method,
        params Measurand[] measurands)
    {
        var relErr = method switch
        {
            ErrorPropagationMethod.Uncorrelated => Math.Sqrt(measurands.Sum(m => m.RelativeError * m.RelativeError)),
            ErrorPropagationMethod.Correlated => measurands.Sum(m => m.RelativeError),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        return GaussianUncertainty.FromRelErr(relErr);
    }

    public IUncertainty PropagateErrorThroughExponentiation(
        Measurand measurand,
        int exponentNumerator,
        int exponentDenominator)
    {
        var relErr = measurand.RelativeError * exponentNumerator / exponentDenominator;
        return GaussianUncertainty.FromRelErr(Math.Abs(relErr));
    }
}