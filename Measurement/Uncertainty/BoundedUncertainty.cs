namespace Measurement.Uncertainty;

/// <summary>
/// Asymmetric absolute uncertainty expressed as upper and lower error magnitudes in KMS units.
/// For example, a mass of 10 kg with <c>BoundedUncertainty(0.2, 0.1)</c> represents the interval [9.9, 10.2] kg.
/// For propagation through arithmetic operations, the larger of the two absolute errors is used
/// as a conservative estimate. Monte Carlo propagation (preserving asymmetry) is deferred to Milestone 4.
/// </summary>
public sealed class BoundedUncertainty : IUncertainty
{
    public double UpperAbsoluteError { get; }
    public double LowerAbsoluteError { get; }

    public BoundedUncertainty(double upperAbsoluteError, double lowerAbsoluteError)
    {
        if (double.IsNegative(upperAbsoluteError) || double.IsNegative(lowerAbsoluteError))
            throw new ArgumentException("Absolute errors cannot be negative.");
        if (double.IsInfinity(upperAbsoluteError) || double.IsNaN(upperAbsoluteError) ||
            double.IsInfinity(lowerAbsoluteError) || double.IsNaN(lowerAbsoluteError))
            throw new ArgumentException("Absolute errors cannot be infinite or NaN.");

        UpperAbsoluteError = upperAbsoluteError;
        LowerAbsoluteError = lowerAbsoluteError;
    }

    public double AbsoluteError(double nominalKmsValue) => Math.Max(UpperAbsoluteError, LowerAbsoluteError);

    public double RelativeError(double nominalKmsValue) =>
        Math.Abs(nominalKmsValue) > 0
            ? AbsoluteError(nominalKmsValue) / Math.Abs(nominalKmsValue)
            : 0;
}
