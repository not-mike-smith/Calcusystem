namespace Measurement.Interfaces;

/// <summary>
/// Marker specialization of <see cref="IUncertainty"/> for the symmetric case, where the error above and
/// below the nominal value is identical. It declares no new members; instead it supplies default interface
/// implementations of <see cref="IUncertainty.UpperAbsoluteError"/> and
/// <see cref="IUncertainty.LowerAbsoluteError"/> in terms of the inherited
/// <see cref="IUncertainty.AbsoluteError"/>, so implementers only need to define the single absolute error.
/// </summary>
public interface ISymmetricUncertainty : IUncertainty
{
    /// <summary>Symmetric default: the upper bound equals <see cref="IUncertainty.AbsoluteError"/>.</summary>
    double IUncertainty.UpperAbsoluteError(double nominalKmsValue) => AbsoluteError(nominalKmsValue);

    /// <summary>Symmetric default: the lower bound equals <see cref="IUncertainty.AbsoluteError"/>.</summary>
    double IUncertainty.LowerAbsoluteError(double nominalKmsValue) => AbsoluteError(nominalKmsValue);
}
