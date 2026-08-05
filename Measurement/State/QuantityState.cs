namespace Measurement.State;

/// <summary>
/// The complete stored state of a <see cref="Quantity"/>: its KMS-normalized value and its dimensionality.
/// </summary>
/// <remarks>
/// <see cref="Dimensionality"/> travels as the struct, not as a formatted string. Rendering it as
/// <c>"M·L²·t⁻²"</c> is a wire-format decision that belongs to the persistence layer — and one that would
/// otherwise oblige this assembly to parse superscripts back out again.
/// </remarks>
/// <param name="KmsValue">The value in SI base (kg-m-s) units.</param>
/// <param name="Dimensionality">The physical dimension of the value.</param>
public readonly record struct QuantityState(double KmsValue, Dimensionality Dimensionality);
