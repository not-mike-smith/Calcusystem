namespace Measurement.Interfaces;

/// <summary>
/// Represents the uncertainty of a physical quantity expressed in KMS units.
/// The uncertainty interval around a nominal value <c>v</c> is
/// <c>[v - LowerAbsoluteError(v), v + UpperAbsoluteError(v)]</c>.
/// </summary>
/// <remarks>
/// Errors are stored as a function of the nominal value rather than as fixed magnitudes: a relative-error
/// implementation scales with <c>v</c>, while an absolute-error implementation ignores it. This is why every
/// member takes <c>nominalKmsValue</c> — the interval cannot be resolved without knowing the value it surrounds.
/// </remarks>
public interface IUncertainty
{
    /// <summary>Absolute error above the nominal value in KMS units.</summary>
    double UpperAbsoluteError(double nominalKmsValue);

    /// <summary>Absolute error below the nominal value in KMS units.  Always a positive value</summary>
    double LowerAbsoluteError(double nominalKmsValue);

    /// <summary>Relative error above the nominal value (a fraction of the nominal value).</summary>
    double UpperRelativeError(double nominalKmsValue);

    /// <summary>
    /// Relative error below the nominal value (a fraction of the nominal value). Always a positive value
    /// </summary>
    double LowerRelativeError(double nominalKmsValue);

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

    /// <summary>
    /// Returns the uncertainty describing the reciprocal (<c>1 / v</c>) of the value this instance describes.
    /// The relative error is preserved, but directional bounds swap: the upper bound of <c>v</c> becomes the
    /// lower bound of <c>1 / v</c>. Symmetric implementations return themselves unchanged; asymmetric ones
    /// swap their upper and lower errors.
    /// </summary>
    /// <param name="nominalKmsValue">The nominal KMS value this uncertainty currently describes.</param>
    IUncertainty Reciprocal(double nominalKmsValue);

    /// <summary>
    /// Returns the uncertainty describing the negation (<c>-v</c>) of the value this instance describes.
    /// As with <see cref="Reciprocal"/>, the magnitude of the relative error is preserved while directional
    /// bounds swap. Symmetric implementations return themselves unchanged; asymmetric ones swap upper and lower.
    /// </summary>
    /// <param name="nominalKmsValue">The nominal KMS value this uncertainty currently describes.</param>
    IUncertainty Negated(double nominalKmsValue);
}
