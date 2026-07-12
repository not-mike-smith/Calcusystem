using Measurement.Interfaces;

namespace Measurement.Uncertainty;

public class ConservativeGaussianPropagator : IErrorPropagator
{
    public static ConservativeGaussianPropagator Instance { get; } = new ConservativeGaussianPropagator();

    public IUncertainty PropagateErrorThroughSum(
        ErrorPropagationMethod method,
        params Measurand[] quantities)
    {
        double absoluteError = method switch
        {
            ErrorPropagationMethod.Uncorrelated => Math.Sqrt(quantities.Sum(m => m.KmsAbsoluteError * m.KmsAbsoluteError)),
            ErrorPropagationMethod.Correlated => quantities.Sum(m => m.KmsAbsoluteError),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        double relErr = absoluteError / quantities.Sum(m => m.Quantity.KmsValue);
        return GaussianUncertainty.FromRelErr(relErr);
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