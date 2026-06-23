namespace Measurement.Uncertainty;

/// <summary>
/// Represents the uncertainty of a physical quantity.
/// All values are expressed in terms of the KMS (kg-m-s) representation of the quantity.
/// </summary>
public interface IUncertainty
{
    /// <summary>Returns the symmetric relative error to use in propagation formulas.
    /// For asymmetric uncertainty types this is a conservative (worst-case) estimate.</summary>
    double RelativeError(double nominalKmsValue);

    /// <summary>Returns the symmetric absolute error in KMS units.
    /// For asymmetric uncertainty types this is a conservative (worst-case) estimate.</summary>
    double AbsoluteError(double nominalKmsValue);
}
