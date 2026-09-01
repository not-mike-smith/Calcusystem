using Calcusystem.Measurement.Dimensions;

namespace Calcusystem.Measurement.State;

/// <summary>
/// The complete stored state of a <see cref="Dimensionality"/>: the exponent of each fundamental dimension
/// present. Zero exponents are stripped, so an empty map is a dimensionless value.
/// </summary>
/// <remarks>
/// <para>
/// Structural, not encoded. How these pairs are written — symbols or names, a nested object or a compact string
/// like <c>"M1,L1,T-2"</c>, and what happens to a payload written before a symbol was renamed — is the
/// persistence layer's decision, and lives there. This assembly only answers what data defines the value.
/// </para>
/// <para>
/// A map is affordable here because a state object lives only for the duration of a serialization or
/// deserialization pass; it is not a representation the rest of the library computes with.
/// </para>
/// </remarks>
/// <param name="Exponents">Exponent per present fundamental dimension; empty (or default) for dimensionless.</param>
public readonly record struct DimensionalityState(IReadOnlyDictionary<FundamentalDimension, int> Exponents)
{
    /// <summary>The exponent pairs, treating a <c>default</c> instance as dimensionless.</summary>
    public IReadOnlyDictionary<FundamentalDimension, int> Pairs =>
        Exponents ?? new Dictionary<FundamentalDimension, int>();

    /// <remarks>
    /// Compares the maps set-wise. The compiler-generated version would compare dictionary <i>references</i>,
    /// which would make two states describing the same dimension unequal — a trap that would propagate into
    /// <see cref="QuantityState"/> and <see cref="MeasurandState"/>, since a record struct's equality is built
    /// from its fields'.
    /// </remarks>
    public bool Equals(DimensionalityState other)
    {
        var mine = Pairs;
        var theirs = other.Pairs;

        return mine.Count == theirs.Count
               && mine.All(pair => theirs.TryGetValue(pair.Key, out var exponent) && exponent == pair.Value);
    }

    /// <inheritdoc cref="Equals(DimensionalityState)"/>
    public override int GetHashCode() =>
        Pairs.Aggregate(0, (hash, pair) => hash ^ HashCode.Combine(pair.Key, pair.Value));
}
