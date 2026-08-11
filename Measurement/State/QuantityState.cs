namespace Calcusystem.Measurement.State;

/// <summary>
/// The complete stored state of a <see cref="Quantity"/>: its KMS-normalized value and its dimensionality.
/// </summary>
/// <remarks>
/// The dimensionality travels as a <see cref="DimensionalityState"/> rather than the <see cref="Dimensionality"/>
/// struct. The struct keeps its exponent map private, so a serializer handed one emits <c>{}</c> and reads back a
/// dimensionless value — silently, with no exception. Exposing the pairs as state means nothing that claims to be
/// serializable state can carry that trap.
/// </remarks>
/// <param name="KmsValue">The value in SI base (kg-m-s) units.</param>
/// <param name="Dimensionality">The physical dimension of the value, as exponent pairs.</param>
public readonly record struct QuantityState(double KmsValue, DimensionalityState Dimensionality);
