using System;
using DimensionedExpression.Interfaces;
using DimensionedExpression.State;

namespace DimensionedExpression.Provenance;

/// <summary>
/// The single creation point for <see cref="IProvenance"/> values. Every provenance kind is created here —
/// read this class to see the full set available. The concrete types are public so callers can pattern-match on
/// a kind, but their constructors are internal and their metadata is internal, so construction always flows
/// through this factory and the metadata leaves the assembly only as a <see cref="ProvenanceState"/>.
/// </summary>
/// <remarks>
/// Every method here generates a fresh identity. Restoring a persisted one is a separate concern with its own
/// door — <see cref="FromState"/> — kept apart from the creation vocabulary so that a caller recording where a
/// value came from is never offered an <c>id</c> parameter that only makes sense to a deserializer.
/// </remarks>
public static class ProvenanceFactory
{
    /// <summary>An instrument or sensor reading; uncertainty characterises instrument calibration/repeatability.</summary>
    public static IProvenance Measured(
        string? instrumentId = null,
        DateOnly? calibrationDate = null) =>
        new MeasuredProvenance(instrumentId, calibrationDate, Constants.CREATE_NEW);

    /// <summary>A literature or tabulated value (physical constant, material/thermodynamic property).</summary>
    public static IProvenance Reference(
        string citation,
        string? url = null,
        int? year = null) =>
        new ReferenceProvenance(citation, url, year, Constants.CREATE_NEW);

    /// <summary>An engineer-specified value; the tolerance, if any, lives in the variable's uncertainty.</summary>
    public static IProvenance Design(
        string? specReference = null) =>
        new DesignProvenance(specReference, Constants.CREATE_NEW);

    /// <summary>An empirically fitted constant within a constitutive relationship (model-specific, not a physical property).</summary>
    public static IProvenance Model(
        string modelName,
        string? fittingReference = null) =>
        new ModelProvenance(modelName, fittingReference, Constants.CREATE_NEW);

    /// <summary>
    /// Rebuilds a provenance from previously captured state, preserving its original identity. The counterpart to
    /// <see cref="IProvenance.GetState"/>, and the reason <see cref="IProvenance"/> does not implement
    /// <c>IStateful</c>: the concrete kind is chosen by inspecting the state, so reconstruction is a static
    /// gateway over the closed set rather than a <c>static abstract</c> on each kind.
    /// </summary>
    /// <remarks>A persistence entry point, deliberately apart from the creation methods above.</remarks>
    public static IProvenance FromState(ProvenanceState state) => state.Kind switch
    {
        ProvenanceKind.Measured =>
            new MeasuredProvenance(state.InstrumentId, state.CalibrationDate, state.Id),
        ProvenanceKind.Reference =>
            new ReferenceProvenance(state.Citation!, state.Url, state.Year, state.Id),
        ProvenanceKind.Design =>
            new DesignProvenance(state.SpecReference, state.Id),
        ProvenanceKind.Model =>
            new ModelProvenance(state.ModelName!, state.FittingReference, state.Id),
        _ => throw new ArgumentOutOfRangeException(
            nameof(state), state.Kind, "Unknown provenance kind."),
    };
}
