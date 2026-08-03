using System;
using System.Text.Json;
using DimensionedExpression.Interfaces;

namespace DimensionedExpression.Provenance;

/// <summary>
/// The single creation point for <see cref="IProvenance"/> values. Every provenance kind is created here and
/// reconstructed here — read this class to see the full set of provenance types available. Concrete
/// implementations are intentionally private so that construction always flows through this factory.
/// </summary>
public static class ProvenanceFactory
{
    /// <summary>An instrument or sensor reading; uncertainty characterises instrument calibration/repeatability.</summary>
    public static IProvenance Measured(string? instrumentId = null, DateOnly? calibrationDate = null) =>
        new MeasuredProvenance { InstrumentId = instrumentId, CalibrationDate = calibrationDate };

    /// <summary>A literature or tabulated value (physical constant, material/thermodynamic property).</summary>
    public static IProvenance Reference(string citation, string? url = null, int? year = null) =>
        new ReferenceProvenance { Citation = citation, Url = url, Year = year };

    /// <summary>An engineer-specified value; the tolerance, if any, lives in the variable's uncertainty.</summary>
    public static IProvenance Design(string? specReference = null) =>
        new DesignProvenance { SpecReference = specReference };

    /// <summary>An empirically fitted constant within a constitutive relationship (model-specific, not a physical property).</summary>
    public static IProvenance Model(string modelName, string? fittingReference = null) =>
        new ModelProvenance { ModelName = modelName, FittingReference = fittingReference };

    /// <summary>
    /// Reconstructs an <see cref="IProvenance"/> from the self-describing payload produced by
    /// <see cref="IProvenance.Serialize"/>. Dispatches on the embedded kind discriminator.
    /// </summary>
    /// <exception cref="NotSupportedException">The payload's kind is not a known provenance type.</exception>
    public static IProvenance Deserialize(string serialized)
    {
        using var document = JsonDocument.Parse(serialized);
        var kind = document.RootElement.GetProperty(nameof(IKinded.Kind)).GetString();

        return kind switch
        {
            MeasuredProvenance.KindValue => JsonSerializer.Deserialize<MeasuredProvenance>(serialized)!,
            ReferenceProvenance.KindValue => JsonSerializer.Deserialize<ReferenceProvenance>(serialized)!,
            DesignProvenance.KindValue => JsonSerializer.Deserialize<DesignProvenance>(serialized)!,
            ModelProvenance.KindValue => JsonSerializer.Deserialize<ModelProvenance>(serialized)!,
            _ => throw new NotSupportedException($"Unknown provenance kind '{kind}'")
        };
    }

    // Kind is serialized (written by the getter) as the discriminator and ignored on read (no setter).
    private interface IKinded
    {
        string Kind { get; }
    }

    private sealed record MeasuredProvenance : IProvenance, IKinded
    {
        public const string KindValue = "measured";
        public string Kind => KindValue;
        public string? InstrumentId { get; init; }
        public DateOnly? CalibrationDate { get; init; }

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

        public string Serialize() => JsonSerializer.Serialize(this);
    }

    private sealed record ReferenceProvenance : IProvenance, IKinded
    {
        public const string KindValue = "reference";
        public string Kind => KindValue;
        public string Citation { get; init; } = "";
        public string? Url { get; init; }
        public int? Year { get; init; }

        public string Summary() =>
            $"Reference: {Citation}{(Year is null ? "" : $" ({Year})")}";

        public string Serialize() => JsonSerializer.Serialize(this);
    }

    private sealed record DesignProvenance : IProvenance, IKinded
    {
        public const string KindValue = "design";
        public string Kind => KindValue;
        public string? SpecReference { get; init; }

        public string Summary() =>
            $"Design parameter{(SpecReference is null ? "" : $" (spec {SpecReference})")}";

        public string Serialize() => JsonSerializer.Serialize(this);
    }

    private sealed record ModelProvenance : IProvenance, IKinded
    {
        public const string KindValue = "model";
        public string Kind => KindValue;
        public string ModelName { get; init; } = "";
        public string? FittingReference { get; init; }

        public string Summary() =>
            $"Model parameter: {ModelName}{(FittingReference is null ? "" : $" (fit {FittingReference})")}";

        public string Serialize() => JsonSerializer.Serialize(this);
    }
}
