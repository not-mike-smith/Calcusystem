using Calcusystem.DimensionedExpression.Enums;

namespace Calcusystem.DimensionedExpression.Snapshots;

/// <summary>
/// The complete stored state of an <see cref="IProvenance"/>: its identity, its kind, and that kind's audit
/// metadata. Read it via <see cref="IProvenance.GetSnapshot"/>; rebuild via <c>ProvenanceFactory.FromSnapshot</c>.
/// </summary>
/// <remarks>
/// <para>
/// The union of every kind's fields, discriminated by <see cref="Type"/> — the kinds differ only in metadata, not
/// behaviour, so a flat shape costs nothing and keeps the seam non-generic. Only the fields belonging to
/// <see cref="Type"/> are populated.
/// </para>
/// <para>
/// A memento, not a DTO: no wire type-name, no schema version, no encoding. This is what lets the concrete kinds
/// keep their metadata <c>internal</c> — before this existed, those properties were public solely so a mapper in
/// another assembly could read them.
/// </para>
/// </remarks>
public readonly record struct ProvenanceSnapshot
{
    /// <summary>Which concrete provenance this state rebuilds into.</summary>
    public ProvenanceType Type { get; private init; }

    /// <summary>Stable identity, preserved across a round trip.</summary>
    public string Id { get; private init; }

    /// <summary><see cref="ProvenanceType.Measured"/>: identifier of the instrument that took the reading.</summary>
    public string? InstrumentId { get; private init; }

    /// <summary><see cref="ProvenanceType.Measured"/>: when that instrument was last calibrated.</summary>
    public DateOnly? CalibrationDate { get; private init; }

    /// <summary><see cref="ProvenanceType.Reference"/>: the source being cited.</summary>
    public string? Citation { get; private init; }

    /// <summary><see cref="ProvenanceType.Reference"/>: link to the source.</summary>
    public string? Url { get; private init; }

    /// <summary><see cref="ProvenanceType.Reference"/>: publication year of the source.</summary>
    public int? Year { get; private init; }

    /// <summary><see cref="ProvenanceType.Design"/>: the specification or drawing the value comes from.</summary>
    public string? SpecReference { get; private init; }

    /// <summary><see cref="ProvenanceType.Model"/>: the model the constant belongs to.</summary>
    public string? ModelName { get; private init; }

    /// <summary><see cref="ProvenanceType.Model"/>: reference for the fitting that produced the constant.</summary>
    public string? FittingReference { get; private init; }

    /// <summary>Captures the state of a measured provenance.</summary>
    public static ProvenanceSnapshot Measured(string id, string? instrumentId, DateOnly? calibrationDate) => new()
    {
        Type = ProvenanceType.Measured,
        Id = id,
        InstrumentId = instrumentId,
        CalibrationDate = calibrationDate,
    };

    /// <summary>Captures the state of a reference provenance.</summary>
    public static ProvenanceSnapshot Reference(string id, string citation, string? url, int? year) => new()
    {
        Type = ProvenanceType.Reference,
        Id = id,
        Citation = citation,
        Url = url,
        Year = year,
    };

    /// <summary>Captures the state of a design provenance.</summary>
    public static ProvenanceSnapshot Design(string id, string? specReference) => new()
    {
        Type = ProvenanceType.Design,
        Id = id,
        SpecReference = specReference,
    };

    /// <summary>Captures the state of a model provenance.</summary>
    public static ProvenanceSnapshot Model(string id, string modelName, string? fittingReference) => new()
    {
        Type = ProvenanceType.Model,
        Id = id,
        ModelName = modelName,
        FittingReference = fittingReference,
    };
}
