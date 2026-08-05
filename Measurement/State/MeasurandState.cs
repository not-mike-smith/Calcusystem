namespace Measurement.State;

/// <summary>
/// The complete stored state of a <see cref="Measurand"/>: its value and its uncertainty.
/// </summary>
/// <param name="Quantity">State of the underlying dimensioned value.</param>
/// <param name="Uncertainty">State of the attached uncertainty.</param>
public readonly record struct MeasurandState(QuantityState Quantity, UncertaintyState Uncertainty);
