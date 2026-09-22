using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Measurement.Snapshots;

/// <summary>
/// The complete snapshot of a <see cref="Measurand"/>: its value and its uncertainty.
/// </summary>
/// <param name="Quantity">Snapshot of the underlying dimensioned value.</param>
/// <param name="Uncertainty">Snapshot of the attached uncertainty.</param>
public readonly record struct MeasurandSnapshot(QuantitySnapshot Quantity, UncertaintySnapshot Uncertainty);
