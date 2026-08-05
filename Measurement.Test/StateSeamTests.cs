using System;
using System.Linq;
using FluentAssertions;
using Measurement.Extensions;
using Measurement.Interfaces;
using Measurement.State;
using Measurement.Units;
using Xunit;

namespace Measurement.Test;

/// <summary>
/// Covers the persistence seam: <see cref="IUncertainty.GetState"/> / <see cref="IStateful{TSelf,TState}"/> out,
/// <see cref="UncertaintyFactory.FromState"/> / <c>FromState</c> back. A round trip must preserve the stored form,
/// not merely an equivalent error band — storing 0 as an absolute error means something different from storing it
/// as a relative one.
/// </summary>
public class StateSeamTests
{
    [Fact]
    public void SymmetricRelativeUncertaintyRoundTrips()
    {
        IUncertainty original = SymmetricUncertainty.FromRelErr(0.02);

        var state = original.GetState();
        state.Shape.Should().Be(UncertaintyShape.Symmetric);
        state.IsStoredAsAbs.Should().BeFalse();

        var rebuilt = UncertaintyFactory.FromState(state);
        rebuilt.Should().BeOfType<SymmetricUncertainty>();
        rebuilt.RelativeError(5.0).Should().Be(0.02);
        rebuilt.AbsoluteError(5.0).Should().Be(0.1);
    }

    [Fact]
    public void SymmetricAbsoluteUncertaintyRoundTripsAndSurvivesAtZero()
    {
        IUncertainty original = SymmetricUncertainty.FromAbsErr(1.0.Units(Mass.Milligram));

        var state = original.GetState();
        state.Shape.Should().Be(UncertaintyShape.Symmetric);
        state.IsStoredAsAbs.Should().BeTrue();

        var rebuilt = UncertaintyFactory.FromState(state);

        // The storage form is what makes an error at zero meaningful; a round trip must not quietly convert it.
        rebuilt.AbsoluteError(0.0).Should().Be(original.AbsoluteError(0.0));
        rebuilt.RelativeError(0.0).Should().Be(double.PositiveInfinity);
    }

    [Fact]
    public void AsymmetricUncertaintyRoundTripsPreservingDirection()
    {
        IUncertainty original = AsymmetricUncertainty.FromRelErr(0.05, 0.01);

        var state = original.GetState();
        state.Shape.Should().Be(UncertaintyShape.Asymmetric);
        state.UpperMagnitude.Should().Be(0.05);
        state.LowerMagnitude.Should().Be(0.01);

        var rebuilt = UncertaintyFactory.FromState(state);
        rebuilt.Should().BeOfType<AsymmetricUncertainty>();
        rebuilt.UpperRelativeError(2.0).Should().Be(0.05);
        rebuilt.LowerRelativeError(2.0).Should().Be(0.01);
    }

    [Fact]
    public void QuantityRoundTripsItsDimensionality()
    {
        var original = new Quantity(9.81, Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time));

        var rebuilt = Quantity.FromState(original.GetState());

        rebuilt.In(Acceleration.MeterPerSecondSquared).Should().Be(9.81);
        rebuilt.Dimensionality.Should().Be(original.Dimensionality);
    }

    [Fact]
    public void DimensionalityEncodesSymbolAndExponentPerEntry()
    {
        var force = Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

        force.GetState().Encoded.Should().Be("M1,L1,T-2");
    }

    [Fact]
    public void DimensionlessEncodesAsEmptyAndRoundTrips()
    {
        Dimensionality.Dimensionless.GetState().Encoded.Should().BeEmpty();
        Dimensionality.FromState(new DimensionalityState("")).Should().Be(Dimensionality.Dimensionless);

        // default(Dimensionality) has no backing map at all; it must encode the same way.
        default(Dimensionality).GetState().Encoded.Should().BeEmpty();
    }

    [Theory]
    [InlineData("M1")]
    [InlineData("M1,L1,T-2")]
    [InlineData("Θ1")]
    [InlineData("M1,I-1,L2,T-3")]
    [InlineData("L-1")]
    public void DimensionalityStateRoundTripsExactly(string encoded)
    {
        var state = new DimensionalityState(encoded);

        Dimensionality.FromState(state).GetState().Encoded.Should().Be(encoded);
    }

    [Fact]
    public void OutOfOrderEncodingIsAcceptedAndNormalized()
    {
        // Reading is tolerant of entry order; writing always emits canonical order, so a file written by an
        // older or hand-edited source converges on re-save rather than staying permanently diff-noisy.
        var parsed = Dimensionality.FromState(new DimensionalityState("T-3,L2,M1,I-1"));

        parsed.GetState().Encoded.Should().Be("M1,I-1,L2,T-3");
    }

    [Fact]
    public void EncodingIsCanonicalRegardlessOfConstructionOrder()
    {
        var oneWay = Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);
        var otherWay = Dimensionality.Length / Dimensionality.Time * Dimensionality.Mass / Dimensionality.Time;

        oneWay.Should().Be(otherWay);
        oneWay.GetState().Encoded.Should().Be(otherWay.GetState().Encoded);
    }

    [Theory]
    [InlineData("M")]           // exponent missing
    [InlineData("1")]           // symbol missing
    [InlineData("Q1")]          // unknown symbol
    [InlineData("M1,M2")]       // duplicate symbol
    [InlineData("Mx")]          // unparseable exponent
    [InlineData("M1,,L1")]      // empty entry
    public void MalformedDimensionalityStateThrowsRatherThanSilentlyDroppingDimensions(string encoded)
    {
        var act = () => Dimensionality.FromState(new DimensionalityState(encoded));

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void FundamentalDimensionSymbolsAreDistinctIgnoringCase()
    {
        // The encoding is keyed on Symbol, so a case-only collision would make a stray ToLower() downstream
        // silently rewrite one dimension into another.
        var symbols = FundamentalDimension.All.Select(f => f.Symbol).ToList();

        symbols.Should().OnlyHaveUniqueItems();
        symbols.Select(s => s.ToLowerInvariant()).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void MeasurandRoundTripsValueAndUncertaintyTogether()
    {
        var original = Mass.Kilogram.Quantity(2).Measurand(AsymmetricUncertainty.FromRelErr(0.05, 0.01));

        var rebuilt = Measurand.FromState(original.GetState());

        rebuilt.In(Mass.Kilogram).Should().Be(2);
        rebuilt.UpperRelativeError.Should().Be(0.05);
        rebuilt.LowerRelativeError.Should().Be(0.01);
    }
}
