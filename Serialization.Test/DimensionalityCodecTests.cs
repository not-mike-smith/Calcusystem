using System.Linq;
using System;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Serialization.Mappers;
using FluentAssertions;

namespace Calcusystem.Serialization.Test;

/// <summary>
/// The wire encoding for a dimensionality. This lives here rather than in <c>Measurement.Test</c> because the
/// encoding is a format decision — which identity to key on, how to lay the pairs out, and what to reject —
/// and format decisions belong to this layer. <c>Measurement</c> supplies only the structural state.
/// </summary>
public class DimensionalityCodecTests
{
    private static string Encode(Dimensionality dimensionality) =>
        DimensionalityCodec.Encode(dimensionality.GetState());

    private static Dimensionality Decode(string encoded) =>
        Dimensionality.FromState(DimensionalityCodec.Decode(encoded));

    [Fact]
    public void EncodesSymbolAndExponentPerEntry()
    {
        var force = Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

        Encode(force).Should().Be("M1,L1,T-2");
    }

    [Fact]
    public void DimensionlessEncodesAsEmptyString()
    {
        Encode(Dimensionality.Dimensionless).Should().BeEmpty();
        Decode("").Should().Be(Dimensionality.Dimensionless);
        Decode(null!).Should().Be(Dimensionality.Dimensionless);
    }

    [Theory]
    [InlineData("M1")]
    [InlineData("M1,L1,T-2")]
    [InlineData("Θ1")]
    [InlineData("M1,I-1,L2,T-3")]
    [InlineData("L-1")]
    public void RoundTripsExactly(string encoded)
    {
        Encode(Decode(encoded)).Should().Be(encoded);
    }

    [Fact]
    public void EncodingIsCanonicalRegardlessOfConstructionOrder()
    {
        var oneWay = Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);
        var otherWay = Dimensionality.Length / Dimensionality.Time * Dimensionality.Mass / Dimensionality.Time;

        oneWay.Should().Be(otherWay);
        Encode(oneWay).Should().Be(Encode(otherWay));
    }

    [Fact]
    public void OutOfOrderEntriesAreAcceptedAndNormalized()
    {
        // Reading tolerates any entry order; writing always emits canonical order, so a hand-edited file
        // converges on re-save rather than staying permanently diff-noisy.
        Encode(Decode("T-3,L2,M1,I-1")).Should().Be("M1,I-1,L2,T-3");
    }

    [Theory]
    [InlineData("M")]           // exponent missing
    [InlineData("1")]           // symbol missing
    [InlineData("Q1")]          // unknown symbol
    [InlineData("M1,M2")]       // duplicate symbol
    [InlineData("Mx")]          // unparseable exponent
    [InlineData("M1,,L1")]      // empty entry
    public void MalformedInputThrowsRatherThanSilentlyDroppingDimensions(string encoded)
    {
        var act = () => DimensionalityCodec.Decode(encoded);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void FundamentalDimensionSymbolsAreDistinctIgnoringCase()
    {
        // The encoding keys on Symbol, so a case-only collision would let a stray case conversion downstream
        // silently rewrite one dimension into another.
        var symbols = FundamentalDimension.All.Select(f => f.Symbol).ToList();

        symbols.Should().OnlyHaveUniqueItems();
        symbols.Select(s => s.ToLowerInvariant()).Should().OnlyHaveUniqueItems();
    }
}
