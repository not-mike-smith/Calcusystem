namespace Measurement.Uncertainty;

/// <summary>
/// Asymmetric relative uncertainty with different bounds above and below the nominal value.
/// For propagation through arithmetic operations, the larger of the two relative errors is used
/// as a conservative estimate. Monte Carlo propagation (preserving asymmetry) is deferred to Milestone 4.
/// </summary>
public sealed class AsymmetricUncertainty : IUncertainty
{
    public double UpperRelativeError { get; }
    public double LowerRelativeError { get; }

    public AsymmetricUncertainty(double upperRelativeError, double lowerRelativeError)
    {
        if (double.IsNegative(upperRelativeError) || double.IsNegative(lowerRelativeError))
            throw new ArgumentException("Relative errors cannot be negative.");
        if (double.IsInfinity(upperRelativeError) || double.IsNaN(upperRelativeError) ||
            double.IsInfinity(lowerRelativeError) || double.IsNaN(lowerRelativeError))
            throw new ArgumentException("Relative errors cannot be infinite or NaN.");

        UpperRelativeError = upperRelativeError;
        LowerRelativeError = lowerRelativeError;
    }

    public double RelativeError(double nominalKmsValue) => Math.Max(UpperRelativeError, LowerRelativeError);

    public double AbsoluteError(double nominalKmsValue) => RelativeError(nominalKmsValue) * Math.Abs(nominalKmsValue);
}
