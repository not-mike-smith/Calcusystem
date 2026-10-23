namespace Calcusystem.Api.Reports;

/// <summary>A value as it is served: in KMS units, with its uncertainty as absolute KMS magnitudes.</summary>
/// <param name="KmsValue">The nominal value, in KMS base units.</param>
/// <param name="Dimensionality">Encoded as <c>"M1,L1,T-2"</c>, empty for dimensionless.</param>
/// <param name="KmsUpperAbsoluteUncertainty">How far above the nominal the band reaches, in KMS units.</param>
/// <param name="KmsLowerAbsoluteUncertainty">How far below it reaches. Equal to the upper where symmetric.</param>
public sealed record MeasurandReport(
    double KmsValue,
    string Dimensionality,
    double KmsUpperAbsoluteUncertainty,
    double KmsLowerAbsoluteUncertainty);
