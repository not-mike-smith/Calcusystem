using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Snapshots;

namespace Calcusystem.Serialization.Mappers;

/// <summary>Reads and writes the flat provenance DTO.</summary>
public static class ProvenanceCodec
{
    /// <summary>The wire form of <paramref name="snapshot"/>.</summary>
    public static Dtos.Provenance ToWire(ProvenanceSnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        Type = WireNames.Of(snapshot.Type),
        InstrumentId = snapshot.InstrumentId,
        CalibrationDate = snapshot.CalibrationDate,
        Citation = snapshot.Citation,
        Url = snapshot.Url,
        Year = snapshot.Year,
        SpecReference = snapshot.SpecReference,
        ModelName = snapshot.ModelName,
        FittingReference = snapshot.FittingReference,
    };

    /// <summary>The snapshot <paramref name="provenance"/> describes, or null where it is null.</summary>
    /// <exception cref="NotImplementedException">The <c>Type</c> names no known kind.</exception>
    public static ProvenanceSnapshot? FromWire(Dtos.Provenance? provenance)
    {
        if (provenance is null) return null;

        return WireNames.ProvenanceTypeOf(provenance.Type) switch
        {
            ProvenanceType.Measured => ProvenanceSnapshot.Measured(
                provenance.Id, provenance.InstrumentId, provenance.CalibrationDate),
            ProvenanceType.Reference => ProvenanceSnapshot.Reference(
                provenance.Id, provenance.Citation!, provenance.Url, provenance.Year),
            ProvenanceType.Design => ProvenanceSnapshot.Design(
                provenance.Id, provenance.SpecReference),
            ProvenanceType.Model => ProvenanceSnapshot.Model(
                provenance.Id, provenance.ModelName!, provenance.FittingReference),
            var kind => throw new NotImplementedException($"No snapshot mapping for provenance kind {kind}")
        };
    }
}
