using System;
using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;

namespace DimensionedExpression.Provenance;

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

    public string? InstrumentId { get; }
    public DateOnly? CalibrationDate { get; }

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
}
