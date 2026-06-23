namespace Measurement.Uncertainty;

/// <summary>
/// Asymmetric absolute uncertainty expressed as independent upper and lower error magnitudes in KMS units.
/// For example, a mass of 10 kg with <c>BoundedUncertainty(upperError: 0.2, lowerError: 0.1)</c>
/// represents the interval [9.9, 10.2] kg.
/// For propagation through arithmetic operations, the larger of the two absolute errors is used
/// as a conservative estimate. Monte Carlo propagation (preserving asymmetry) is deferred to Milestone 4.
/// </summary>
public sealed class BoundedUncertainty : IUncertainty
{
    /// <summary>Absolute error above the nominal value in KMS units.</summary>
    public double UpperError { get; }

    /// <summary>Absolute error below the nominal value in KMS units.</summary>
    public double LowerError { get; }

    public BoundedUncertainty(double upperError, double lowerError)
    {
        if (double.IsNegative(upperError) || double.IsNegative(lowerError))
            throw new ArgumentException("Absolute errors cannot be negative.");
        if (double.IsInfinity(upperError) || double.IsNaN(upperError) ||
            double.IsInfinity(lowerError) || double.IsNaN(lowerError))
            throw new ArgumentException("Absolute errors cannot be infinite or NaN.");

        UpperError = upperError;
        LowerError = lowerError;
    }

    public double UpperAbsoluteError(double nominalKmsValue) => UpperError;
    public double LowerAbsoluteError(double nominalKmsValue) => LowerError;

    public double RelativeError(double nominalKmsValue) =>
        Math.Abs(nominalKmsValue) > 0
            ? Math.Max(UpperError, LowerError) / Math.Abs(nominalKmsValue)
            : 0;
}
