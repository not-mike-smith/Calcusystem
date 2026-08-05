namespace Measurement.Interfaces;

/// <summary>
/// Marker specialization of <see cref="IUncertainty"/> for the symmetric case, where the error above and
/// below the nominal value is identical. It declares no new members; instead it supplies default interface
/// implementations of Upper and Lower error members in terms of the single Absolute and Relative error members,
/// so that implementers of symmetric uncertainty only need to define those two.
/// </summary>
public interface ISymmetricUncertainty : IUncertainty
{
    /// <summary>Symmetric default: the upper bound equals <see cref="IUncertainty.AbsoluteError"/>.</summary>
    double IUncertainty.UpperAbsoluteError(double nominalKmsValue) => AbsoluteError(nominalKmsValue);

    /// <summary>Symmetric default: the lower bound equals <see cref="IUncertainty.AbsoluteError"/>.</summary>
    double IUncertainty.LowerAbsoluteError(double nominalKmsValue) => AbsoluteError(nominalKmsValue);

    /// <summary>
    /// Symmetric default: the upper bound equals <see cref="IUncertainty.RelativeError"/>.
    /// </summary>
    double IUncertainty.UpperRelativeError(double nominalKmsValue) => RelativeError(nominalKmsValue);

    /// <summary>
    /// Symmetric default: the lower bound equals <see cref="IUncertainty.RelativeError"/>.
    /// </summary>
    double IUncertainty.LowerRelativeError(double nominalKmsValue) => RelativeError(nominalKmsValue);
}
