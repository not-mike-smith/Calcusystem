namespace Measurement.Uncertainty;

/// <summary>
/// How an uncertainty stores its error internally. The stored form is an implementation detail — consumers read
/// absolute or relative error through <see cref="Measurement.Interfaces.IUncertainty"/> regardless — but it
/// determines behaviour at zero: an absolute error is well-defined there, a relative one is not.
/// </summary>
public enum UncertaintyKind
{
    /// <summary>Stored as a relative error (a fraction of the nominal value). Undefined when the value is zero.</summary>
    Relative,

    /// <summary>Stored as an absolute error in KMS units; well-defined even when the nominal value is zero.</summary>
    Absolute,

    // Reserved for a future interval-native representation (explicit KMS bounds).
}
