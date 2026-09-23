namespace Calcusystem.Measurement.Interfaces;

/// <summary>
/// Marker specialization of <see cref="IUncertainty"/> for the symmetric case, where the uncertainty above and
/// below the nominal value is identical. It declares no new members; instead it supplies default interface
/// implementations of Upper and Lower uncertainty members in terms of the single Absolute and Relative uncertainty members,
/// so that implementers of symmetric uncertainty only need to define those two.
/// </summary>
public interface ISymmetricUncertainty : IUncertainty
{
    /// <summary>Symmetric default: the upper bound equals <see cref="IUncertainty.AbsoluteUncertainty"/>.</summary>
    double IUncertainty.UpperAbsoluteUncertainty(double nominalKmsValue) => AbsoluteUncertainty(nominalKmsValue);

    /// <summary>Symmetric default: the lower bound equals <see cref="IUncertainty.AbsoluteUncertainty"/>.</summary>
    double IUncertainty.LowerAbsoluteUncertainty(double nominalKmsValue) => AbsoluteUncertainty(nominalKmsValue);

    /// <summary>
    /// Symmetric default: the upper bound equals <see cref="IUncertainty.RelativeUncertainty"/>.
    /// </summary>
    double IUncertainty.UpperRelativeUncertainty(double nominalKmsValue) => RelativeUncertainty(nominalKmsValue);

    /// <summary>
    /// Symmetric default: the lower bound equals <see cref="IUncertainty.RelativeUncertainty"/>.
    /// </summary>
    double IUncertainty.LowerRelativeUncertainty(double nominalKmsValue) => RelativeUncertainty(nominalKmsValue);
}
