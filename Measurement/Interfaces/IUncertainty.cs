namespace Measurement.Interfaces;

/// <summary>
/// Represents the uncertainty of a physical quantity expressed in KMS units.
/// The uncertainty interval around a nominal value <c>v</c> is
/// <c>[v - LowerAbsoluteError(v), v + UpperAbsoluteError(v)]</c>.
/// </summary>
public interface IUncertainty // TODO: should this be internal?
{
    /// <summary>Absolute error above the nominal value in KMS units.</summary>
    double UpperAbsoluteError(double nominalKmsValue);

    /// <summary>Absolute error below the nominal value in KMS units.</summary>
    double LowerAbsoluteError(double nominalKmsValue);

    /// <summary>
    /// Conservative relative error for use in propagation formulas.
    /// For asymmetric uncertainty types this is the larger of upper and lower relative errors.
    /// </summary>
    double RelativeError(double nominalKmsValue);

    /// <summary>
    /// Conservative absolute error for use in propagation formulas.
    /// For asymmetric uncertainty types this is the larger of upper and lower absolute errors.
    /// </summary>
    double AbsoluteError(double nominalKmsValue);

    IUncertainty Reciprocal(double nominalKmsValue);
    IUncertainty Negated(double nominalKmsValue);
}
