namespace Calcusystem.Api.Reports;

/// <summary>Every unit a client can offer, grouped by what it measures.</summary>
public sealed record UnitCatalogueReport(IReadOnlyList<DimensionalityUnitsReport> Dimensionalities);

/// <summary>The units for one dimensionality.</summary>
/// <param name="Dimensionality">Encoded as <c>"M1,L1,T-2"</c>, empty for dimensionless.</param>
/// <param name="BaseSymbol">
/// The unit a KMS value is already in — the one whose conversion is the identity. A sensible default for a
/// picker, and null where this dimensionality has no such unit.
/// </param>
/// <param name="Units">Every unit of this dimensionality.</param>
public sealed record DimensionalityUnitsReport(
    string Dimensionality,
    string? BaseSymbol,
    IReadOnlyList<UnitReport> Units);

/// <summary>
/// One unit, carrying the same two numbers <c>UnitOfMeasure</c> converts with, under the same names.
/// </summary>
/// <remarks>
/// <para>
/// A client reads a KMS value as <c>kms / KmsConversionFactor - ZeroOffset</c> and writes one as
/// <c>(shown + ZeroOffset) × KmsConversionFactor</c> — which is <c>ConvertFromKmsValue</c> and
/// <c>ConvertToKmsValue</c>, spelled the same way on both sides so the two can be read against each other.
/// </para>
/// <para>
/// <b>A difference is converted by the factor alone.</b> Applying <see cref="ZeroOffset"/> to a band would read
/// ±1 K as −457.87 °F. <see cref="DeltaSymbol"/> is what to call the band's unit where that shows.
/// </para>
/// </remarks>
/// <param name="Symbol">What to show, and what the domain calls it.</param>
/// <param name="KmsConversionFactor">How many KMS base units one of this unit is. Positive and non-zero.</param>
/// <param name="ZeroOffset">
/// Where this unit's zero sits, in this unit's own steps — 273.15 for degrees Celsius, and zero for all but the
/// offset units.
/// </param>
/// <param name="DeltaSymbol">
/// What a difference in this unit is called, where that differs from <see cref="Symbol"/> — <c>Δ°C</c> for
/// degrees Celsius. Null where a difference is named the same as a value.
/// </param>
public sealed record UnitReport(
    string Symbol,
    double KmsConversionFactor,
    double ZeroOffset,
    string? DeltaSymbol);
