using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Measurement.Snapshots;

/// <summary>
/// The complete snapshot of a <see cref="Dimensionality"/>: the exponent of each fundamental dimension
/// present. Zero exponents are stripped, so an empty map is a dimensionless value.
/// </summary>
/// <param name="Exponents">Exponent per present fundamental dimension; empty (or default) for dimensionless.</param>
public readonly record struct DimensionalitySnapshot(IReadOnlyDictionary<FundamentalDimension, int> Exponents)
{
    /// <summary>The exponent pairs, treating a <c>default</c> instance as dimensionless.</summary>
    public IReadOnlyDictionary<FundamentalDimension, int> Pairs =>
        Exponents ?? new Dictionary<FundamentalDimension, int>();

    /// <remarks>
    /// Compares the maps set-wise. The compiler-generated version would compare dictionary <i>references</i>,
    /// which would make two states describing the same dimension unequal — a trap that would propagate into
    /// <see cref="QuantitySnapshot"/> and <see cref="MeasurandSnapshot"/>, since a record struct's equality is built
    /// from its fields'.
    /// </remarks>
    public bool Equals(DimensionalitySnapshot other)
    {
        var mine = Pairs;
        var theirs = other.Pairs;

        return mine.Count == theirs.Count
               && mine.All(pair => theirs.TryGetValue(pair.Key, out var exponent) && exponent == pair.Value);
    }

    /// <inheritdoc cref="Equals(DimensionalitySnapshot)"/>
    public override int GetHashCode() =>
        Pairs.Aggregate(0, (hash, pair) => hash ^ HashCode.Combine(pair.Key, pair.Value));
}
