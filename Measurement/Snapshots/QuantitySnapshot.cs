using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Measurement.Snapshots;

/// <summary>
/// The complete snapshot of a <see cref="Quantity"/>: its KMS-normalized value and its dimensionality.
/// </summary>
/// <remarks>
/// The dimensionality travels as a <see cref="DimensionalitySnapshot"/> rather than the <see cref="Dimensionality"/>
/// struct.
/// </remarks>
/// <param name="KmsValue">The value in SI base (kg-m-s) units.</param>
/// <param name="Dimensionality">The physical dimension of the value, as exponent pairs.</param>
public readonly record struct QuantitySnapshot(double KmsValue, DimensionalitySnapshot Dimensionality);
