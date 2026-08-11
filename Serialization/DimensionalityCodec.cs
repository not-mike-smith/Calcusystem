using Calcusystem.Measurement;
using Calcusystem.Measurement.State;

namespace Calcusystem.Serialization;

/// <summary>
/// The wire encoding for a <see cref="Dimensionality"/>: each present dimension's symbol followed by its integer
/// exponent, comma-separated and in canonical dimension order — force is <c>"M1,L1,T-2"</c>, and a dimensionless
/// value is the empty string.
/// </summary>
/// <remarks>
/// <para>
/// This lives here, not in <c>Measurement</c>, because it is a format decision: which identity to key on
/// (symbols), how to lay the pairs out, and what to do with a payload written before a symbol was renamed.
/// <c>Measurement</c> supplies only the structural <see cref="DimensionalityState"/>.
/// </para>
/// <para>
/// A compact string rather than a nested object because the content is tightly constrained — nine possible
/// symbols and small integer exponents — and a string needs no custom converter in any serializer.
/// </para>
/// <para>
/// <b>The symbols are the contract.</b> Renaming a <see cref="FundamentalDimension"/> symbol invalidates
/// previously written data, and migrating it is this layer's responsibility. The symbol set is distinct even
/// ignoring case, so a stray case conversion downstream cannot rewrite one dimension into another.
/// </para>
/// </remarks>
public static class DimensionalityCodec
{
    private static readonly IReadOnlyDictionary<string, FundamentalDimension> BySymbol =
        FundamentalDimension.All.ToDictionary(f => f.Symbol, StringComparer.Ordinal);

    /// <summary>
    /// Writes the canonical encoding. <see cref="Dimensionality.GetState"/> yields its pairs in canonical order,
    /// so dimensionally-equal values always produce the identical string — safe to diff, compare, or hash.
    /// </summary>
    public static string Encode(DimensionalityState state) =>
        string.Join(',', state.Pairs.Select(pair => $"{pair.Key.Symbol}{pair.Value}"));

    /// <summary>
    /// Reads an encoding produced by <see cref="Encode"/>. Entry order is not significant on the way in;
    /// <see cref="Encode"/> re-normalizes it, so a hand-edited file converges on re-save.
    /// </summary>
    /// <exception cref="FormatException">
    /// The encoding is malformed, names a symbol that is not a known <see cref="FundamentalDimension"/>, or
    /// repeats one. Deserialization fails loudly rather than dropping a dimension: a quietly dimensionless
    /// quantity is far worse than a rejected load.
    /// </exception>
    public static DimensionalityState Decode(string? encoded)
    {
        var pairs = new Dictionary<FundamentalDimension, int>();
        if (string.IsNullOrWhiteSpace(encoded)) return new DimensionalityState(pairs);

        foreach (var token in encoded.Split(',', StringSplitOptions.TrimEntries))
        {
            var splitAt = token.TakeWhile(char.IsLetter).Count();
            if (splitAt == 0 || splitAt == token.Length)
                throw new FormatException($"Malformed dimensionality entry '{token}' in '{encoded}'.");

            var symbol = token[..splitAt];
            if (! BySymbol.TryGetValue(symbol, out var dimension))
                throw new FormatException($"Unknown fundamental dimension symbol '{symbol}' in '{encoded}'.");

            if (! int.TryParse(token[splitAt..], out var exponent))
                throw new FormatException($"Malformed exponent in dimensionality entry '{token}'.");

            if (! pairs.TryAdd(dimension, exponent))
                throw new FormatException($"Duplicate symbol '{symbol}' in '{encoded}'.");
        }

        return new DimensionalityState(pairs);
    }
}
