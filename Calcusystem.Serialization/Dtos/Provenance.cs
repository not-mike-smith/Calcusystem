using System;
using Calcusystem.Serialization.Interfaces;

namespace Calcusystem.Serialization.Dtos;

/// <summary>
/// Flattened serialized form of an <c>IProvenance</c>. <see cref="Type"/> discriminates the kind; only the
/// fields relevant to that kind are populated. Owned inline by a single node (no id-based reference resolution),
/// but carries its <see cref="Id"/> for round-trip fidelity.
/// </summary>
public class Provenance : ISerializedObject
{
    public required string Id { get; init; }
    public required string Type { get; init; }

    // measured
    public string? InstrumentId { get; init; }
    public DateOnly? CalibrationDate { get; init; }

    // reference
    public string? Citation { get; init; }
    public string? Url { get; init; }
    public int? Year { get; init; }

    // design
    public string? SpecReference { get; init; }

    // model
    public string? ModelName { get; init; }
    public string? FittingReference { get; init; }
}
