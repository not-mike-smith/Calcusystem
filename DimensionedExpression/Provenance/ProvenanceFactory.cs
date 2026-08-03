using System;
using DimensionedExpression.Interfaces;

namespace DimensionedExpression.Provenance;

/// <summary>
/// The single creation point for <see cref="IProvenance"/> values. Every provenance kind is created here —
/// read this class to see the full set available. The concrete types are public (so serialization can map
/// them) but their constructors are internal, so construction always flows through this factory.
/// </summary>
/// <remarks>
/// Each method accepts an optional <c>id</c> so that deserialization can restore a provenance's original
/// identity; the default generates a fresh one.
/// </remarks>
public static class ProvenanceFactory
{
    /// <summary>An instrument or sensor reading; uncertainty characterises instrument calibration/repeatability.</summary>
    public static IProvenance Measured(
        string? instrumentId = null,
        DateOnly? calibrationDate = null,
        string id = Constants.CREATE_NEW) =>
        new MeasuredProvenance(instrumentId, calibrationDate, id);

    /// <summary>A literature or tabulated value (physical constant, material/thermodynamic property).</summary>
    public static IProvenance Reference(
        string citation,
        string? url = null,
        int? year = null,
        string id = Constants.CREATE_NEW) =>
        new ReferenceProvenance(citation, url, year, id);

    /// <summary>An engineer-specified value; the tolerance, if any, lives in the variable's uncertainty.</summary>
    public static IProvenance Design(
        string? specReference = null,
        string id = Constants.CREATE_NEW) =>
        new DesignProvenance(specReference, id);

    /// <summary>An empirically fitted constant within a constitutive relationship (model-specific, not a physical property).</summary>
    public static IProvenance Model(
        string modelName,
        string? fittingReference = null,
        string id = Constants.CREATE_NEW) =>
        new ModelProvenance(modelName, fittingReference, id);
}
