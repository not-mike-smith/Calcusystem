namespace Measurement.Uncertainty;

/// <summary>
/// Symmetric relative uncertainty, equivalent to the original single-value model.
/// Propagates via RSS (uncorrelated) or direct sum (correlated) through arithmetic operations.
/// </summary>
public sealed class GaussianUncertainty : ISymmetricUncertainty
{
    private readonly double _relativeError;

    public GaussianUncertainty(double relativeError)
    {
        if (double.IsNegative(relativeError) || double.IsInfinity(relativeError) || double.IsNaN(relativeError))
            throw new ArgumentException("Relative error cannot be negative, infinite, or NaN.", nameof(relativeError));

        _relativeError = relativeError;
    }

    public double RelativeError(double nominalKmsValue) => _relativeError;

    public double AbsoluteError(double nominalKmsValue) => _relativeError * Math.Abs(nominalKmsValue);
}
