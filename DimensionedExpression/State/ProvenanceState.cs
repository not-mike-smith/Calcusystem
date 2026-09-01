using Calcusystem.DimensionedExpression.Enums;

namespace Calcusystem.DimensionedExpression.State;

/// <summary>
/// The complete stored state of an <see cref="IProvenance"/>: its identity, its kind, and that kind's audit
/// metadata. Read it via <see cref="IProvenance.GetState"/>; rebuild via <c>ProvenanceFactory.FromState</c>.
/// </summary>
/// <remarks>
/// <para>
/// The union of every kind's fields, discriminated by <see cref="Kind"/> — the kinds differ only in metadata, not
/// behaviour, so a flat shape costs nothing and keeps the seam non-generic. Only the fields belonging to
/// <see cref="Kind"/> are populated.
/// </para>
/// <para>
/// A memento, not a DTO: no wire type-name, no schema version, no encoding. This is what lets the concrete kinds
/// keep their metadata <c>internal</c> — before this existed, those properties were public solely so a mapper in
/// another assembly could read them.
/// </para>
/// </remarks>
public readonly record struct ProvenanceState
{
    /// <summary>Which concrete provenance this state rebuilds into.</summary>
    public ProvenanceKind Kind { get; private init; }

    /// <summary>Stable identity, preserved across a round trip.</summary>
    public string Id { get; private init; }

    /// <summary><see cref="ProvenanceKind.Measured"/>: identifier of the instrument that took the reading.</summary>
    public string? InstrumentId { get; private init; }

    /// <summary><see cref="ProvenanceKind.Measured"/>: when that instrument was last calibrated.</summary>
    public DateOnly? CalibrationDate { get; private init; }

    /// <summary><see cref="ProvenanceKind.Reference"/>: the source being cited.</summary>
    public string? Citation { get; private init; }

    /// <summary><see cref="ProvenanceKind.Reference"/>: link to the source.</summary>
    public string? Url { get; private init; }

    /// <summary><see cref="ProvenanceKind.Reference"/>: publication year of the source.</summary>
    public int? Year { get; private init; }

    /// <summary><see cref="ProvenanceKind.Design"/>: the specification or drawing the value comes from.</summary>
    public string? SpecReference { get; private init; }

    /// <summary><see cref="ProvenanceKind.Model"/>: the model the constant belongs to.</summary>
    public string? ModelName { get; private init; }

    /// <summary><see cref="ProvenanceKind.Model"/>: reference for the fitting that produced the constant.</summary>
    public string? FittingReference { get; private init; }

    /// <summary>Captures the state of a measured provenance.</summary>
    public static ProvenanceState Measured(string id, string? instrumentId, DateOnly? calibrationDate) => new()
    {
        Kind = ProvenanceKind.Measured,
        Id = id,
        InstrumentId = instrumentId,
        CalibrationDate = calibrationDate,
    };

    /// <summary>Captures the state of a reference provenance.</summary>
    public static ProvenanceState Reference(string id, string citation, string? url, int? year) => new()
    {
        Kind = ProvenanceKind.Reference,
        Id = id,
        Citation = citation,
        Url = url,
        Year = year,
    };

    /// <summary>Captures the state of a design provenance.</summary>
    public static ProvenanceState Design(string id, string? specReference) => new()
    {
        Kind = ProvenanceKind.Design,
        Id = id,
        SpecReference = specReference,
    };

    /// <summary>Captures the state of a model provenance.</summary>
    public static ProvenanceState Model(string id, string modelName, string? fittingReference) => new()
    {
        Kind = ProvenanceKind.Model,
        Id = id,
        ModelName = modelName,
        FittingReference = fittingReference,
    };
}
