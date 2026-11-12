using Calcusystem.Measurement.Snapshots;

namespace Calcusystem.Serialization.Mappers;

/// <summary>Reads a value written as a KMS number, an encoded dimensionality and a flat uncertainty.</summary>
internal static class MeasurandCodec
{
    /// <summary>The measurand those three fields describe.</summary>
    /// <param name="kmsValue">The value in KMS units.</param>
    /// <param name="dimensionality">The encoded dimensionality. Null reads as dimensionless.</param>
    /// <param name="uncertainty">The flat uncertainty. Null reads as exact.</param>
    /// <exception cref="FormatException">The dimensionality is malformed.</exception>
    internal static MeasurandSnapshot FromWire(double kmsValue, string? dimensionality, Dtos.Uncertainty? uncertainty) =>
        new(new QuantitySnapshot(kmsValue, DimensionalityCodec.Decode(dimensionality)), UncertaintyCodec.FromWire(uncertainty));
}
