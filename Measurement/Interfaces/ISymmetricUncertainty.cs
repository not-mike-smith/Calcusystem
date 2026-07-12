namespace Measurement.Interfaces;

/// <summary>
/// Symmetric uncertainty where the error above and below the nominal value is identical.
/// Provides a single <see cref="AbsoluteError"/> and satisfies <see cref="IUncertainty"/>
/// via default interface implementations.
/// </summary>
public interface ISymmetricUncertainty : IUncertainty
{
    double IUncertainty.UpperAbsoluteError(double nominalKmsValue) => AbsoluteError(nominalKmsValue);
    double IUncertainty.LowerAbsoluteError(double nominalKmsValue) => AbsoluteError(nominalKmsValue);
}
