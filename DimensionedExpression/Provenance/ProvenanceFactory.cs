using System;
using Calcusystem.Core.Identity;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Core.Interfaces;

namespace Calcusystem.DimensionedExpression.Provenance;

/// <summary>
/// The single creation point for <see cref="IProvenance"/> values. Every provenance kind is created here —
/// read this class to see the full set available. The concrete types are public so callers can pattern-match on
/// a kind, but their constructors are internal and their metadata is internal, so construction always flows
/// through this factory and the metadata leaves the assembly only as a <see cref="ProvenanceSnapshot"/>.
/// </summary>
/// <remarks>
/// Every method here generates a fresh identity. Restoring a persisted one is a separate concern with its own
/// door — <see cref="FromSnapshot"/> — kept apart from the creation vocabulary so that a caller recording where a
/// value came from is never offered an <c>id</c> parameter that only makes sense to a deserializer.
/// </remarks>
public static class ProvenanceFactory
{
    /// <summary>An instrument or sensor reading; uncertainty characterises instrument calibration/repeatability.</summary>
    public static IProvenance Measured(
        string? instrumentId = null,
        DateOnly? calibrationDate = null) =>
        new MeasuredProvenance(instrumentId, calibrationDate, IdBase.CREATE_NEW_ID);

    /// <summary>A literature or tabulated value (physical constant, material/thermodynamic property).</summary>
    public static IProvenance Reference(
        string citation,
        string? url = null,
        int? year = null) =>
        new ReferenceProvenance(citation, url, year, IdBase.CREATE_NEW_ID);

    /// <summary>An engineer-specified value; the tolerance, if any, lives in the variable's uncertainty.</summary>
    public static IProvenance Design(
        string? specReference = null) =>
        new DesignProvenance(specReference, IdBase.CREATE_NEW_ID);

    /// <summary>An empirically fitted constant within a constitutive relationship (model-specific, not a physical property).</summary>
    public static IProvenance Model(
        string modelName,
        string? fittingReference = null) =>
        new ModelProvenance(modelName, fittingReference, IdBase.CREATE_NEW_ID);

    /// <summary>
    /// Rebuilds a provenance from a previously captured snapshot, preserving its original identity. The counterpart to
    /// <see cref="IProvenance.GetSnapshot"/>, and the reason <see cref="IProvenance"/> does not implement
    /// <see cref="ISnapshotting{TSelf,TSnapshot}"/>: the concrete kind is chosen by inspecting the snapshot, so reconstruction is a static
    /// gateway over the closed set rather than a <c>static abstract</c> on each kind.
    /// </summary>
    /// <remarks>A persistence entry point, deliberately apart from the creation methods above.</remarks>
    public static IProvenance FromSnapshot(ProvenanceSnapshot snapshot) => snapshot.Type switch
    {
        ProvenanceType.Measured =>
            new MeasuredProvenance(snapshot.InstrumentId, snapshot.CalibrationDate, snapshot.Id),
        ProvenanceType.Reference =>
            new ReferenceProvenance(snapshot.Citation!, snapshot.Url, snapshot.Year, snapshot.Id),
        ProvenanceType.Design =>
            new DesignProvenance(snapshot.SpecReference, snapshot.Id),
        ProvenanceType.Model =>
            new ModelProvenance(snapshot.ModelName!, snapshot.FittingReference, snapshot.Id),
        _ => throw new ArgumentOutOfRangeException(
            nameof(snapshot), snapshot.Type, "Unknown provenance kind."),
    };
}
