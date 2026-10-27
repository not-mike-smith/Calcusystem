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
/// One unit, as the two numbers that convert a KMS value into it: <c>shown = Slope × kms + Offset</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every unit in this library is affine in KMS, so those two numbers are the whole of its conversion. They are
/// measured from the unit's own <c>ConvertFromKmsValue</c> rather than read out of it, and
/// <c>UnitCatalogueTests</c> checks that applying them reproduces what the unit itself answers — for every
/// unit served, not a sample.
/// </para>
/// <para>
/// <b>An uncertainty is a difference, so it converts by <see cref="Slope"/> alone.</b> Applying
/// <see cref="Offset"/> to a band would turn ±1 K into ±33.8 °F. <see cref="DeltaSymbol"/> is what to call the
/// band's unit where that distinction is visible.
/// </para>
/// </remarks>
/// <param name="Symbol">What to show, and what the domain calls it.</param>
/// <param name="Slope">The scale from KMS into this unit.</param>
/// <param name="Offset">Where this unit's zero sits, and zero for all but the offset units.</param>
/// <param name="DeltaSymbol">
/// What a difference in this unit is called, where that differs from <see cref="Symbol"/> — <c>Δ°C</c> for
/// degrees Celsius. Null where a difference is named the same as a value.
/// </param>
public sealed record UnitReport(string Symbol, double Slope, double Offset, string? DeltaSymbol);
