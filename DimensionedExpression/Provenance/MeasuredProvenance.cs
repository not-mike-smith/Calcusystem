using System;
using Calcusystem.Core.Identity;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;

namespace Calcusystem.DimensionedExpression.Provenance;

/// <summary>
/// Provenance for an instrument or sensor reading. Construct via <see cref="ProvenanceFactory.Measured"/>.
/// </summary>
public sealed class MeasuredProvenance : IdBase, IProvenance
{
    internal MeasuredProvenance(string? instrumentId, DateOnly? calibrationDate, string id)
        : base(id)
    {
        InstrumentId = instrumentId;
        CalibrationDate = calibrationDate;
    }

    internal string? InstrumentId { get; }
    internal DateOnly? CalibrationDate { get; }

    public string Summary()
    {
        var detail = (InstrumentId, CalibrationDate) switch
        {
            (null, null) => "",
            (not null, null) => $" (instrument {InstrumentId})",
            (null, not null) => $" (calibrated {CalibrationDate.Value:yyyy-MM-dd})",
            _ => $" (instrument {InstrumentId}, calibrated {CalibrationDate.Value:yyyy-MM-dd})"
        };
        return $"Measured{detail}";
    }

    ProvenanceSnapshot IProvenance.GetSnapshot() =>
        ProvenanceSnapshot.Measured(Id, InstrumentId, CalibrationDate);
}
