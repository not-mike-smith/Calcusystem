namespace Measurement.State;

/// <summary>
/// The complete stored state of a <see cref="Dimensionality"/>, as a compact canonical string:
/// each present fundamental dimension's symbol followed by its integer exponent, comma-separated
/// (e.g. force is <c>"M1,L1,T-2"</c>). A dimensionless value encodes as the empty string.
/// </summary>
/// <remarks>
/// <para>
/// A string rather than an exponent map, because the content is tightly constrained — nine possible symbols and
/// small integer exponents — and every serializer handles a string natively without a custom converter. This is
/// deliberately <i>not</i> <see cref="Dimensionality.ToString"/>: that is a human-readable form with middots,
/// superscripts, and a numerator/denominator split, none of which round-trips cleanly.
/// </para>
/// <para>
/// Entries are ordered by <see cref="FundamentalDimension.Order"/> and zero exponents are stripped, so two
/// dimensionally-equal values always encode to the identical string — persisted files diff cleanly and the
/// encoding is safe to hash or compare.
/// </para>
/// <para>
/// The symbols are the wire contract: renaming one invalidates previously persisted data, and repairing that is
/// the persistence layer's job (see the migration note in <c>Calcusystem.Serialization</c>).
/// </para>
/// </remarks>
/// <param name="Encoded">The canonical encoding; empty for a dimensionless value.</param>
public readonly record struct DimensionalityState(string Encoded);
