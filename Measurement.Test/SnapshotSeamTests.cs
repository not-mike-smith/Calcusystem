using System.Linq;
using System;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Factories;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Measurement.Test;

/// <summary>
/// Covers the persistence seam: <see cref="IUncertainty.GetSnapshot"/> / <see cref="ISnapshotting{TSelf,TSnapshot}"/> out,
/// <see cref="UncertaintyFactory.FromSnapshot"/> / <c>FromSnapshot</c> back. A round trip must preserve the stored form,
/// not merely an equivalent error band — storing 0 as an absolute error means something different from storing it
/// as a relative one.
/// </summary>
public class SnapshotSeamTests
{
    [Fact]
    public void SymmetricRelativeUncertaintyRoundTrips()
    {
        IUncertainty original = SymmetricUncertainty.FromRelative(0.02);

        var state = original.GetSnapshot();
        state.Type.Should().Be(UncertaintyType.Symmetric);
        state.IsStoredAsAbs.Should().BeFalse();

        var rebuilt = UncertaintyFactory.FromSnapshot(state);
        rebuilt.Should().BeOfType<SymmetricUncertainty>();
        rebuilt.RelativeUncertainty(5.0).Should().Be(0.02);
        rebuilt.AbsoluteUncertainty(5.0).Should().Be(0.1);
    }

    [Fact]
    public void SymmetricAbsoluteUncertaintyRoundTripsAndSurvivesAtZero()
    {
        IUncertainty original = SymmetricUncertainty.FromAbsolute(1.0.Units(Mass.Milligram));

        var state = original.GetSnapshot();
        state.Type.Should().Be(UncertaintyType.Symmetric);
        state.IsStoredAsAbs.Should().BeTrue();

        var rebuilt = UncertaintyFactory.FromSnapshot(state);

        // The storage form is what makes an error at zero meaningful; a round trip must not quietly convert it.
        rebuilt.AbsoluteUncertainty(0.0).Should().Be(original.AbsoluteUncertainty(0.0));
        rebuilt.RelativeUncertainty(0.0).Should().Be(double.PositiveInfinity);
    }

    [Fact]
    public void AsymmetricUncertaintyRoundTripsPreservingDirection()
    {
        IUncertainty original = AsymmetricUncertainty.FromRelative(0.05, 0.01);

        var state = original.GetSnapshot();
        state.Type.Should().Be(UncertaintyType.Asymmetric);
        state.UpperMagnitude.Should().Be(0.05);
        state.LowerMagnitude.Should().Be(0.01);

        var rebuilt = UncertaintyFactory.FromSnapshot(state);
        rebuilt.Should().BeOfType<AsymmetricUncertainty>();
        rebuilt.UpperRelativeUncertainty(2.0).Should().Be(0.05);
        rebuilt.LowerRelativeUncertainty(2.0).Should().Be(0.01);
    }

    [Fact]
    public void QuantityRoundTripsItsDimensionality()
    {
        var original = new Quantity(9.81, Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time));

        var rebuilt = Quantity.FromSnapshot(original.GetSnapshot());

        rebuilt.In(Acceleration.MeterPerSecondSquared).Should().Be(9.81);
        rebuilt.Dimensionality.Should().Be(original.Dimensionality);
    }

    [Fact]
    public void DimensionalityStateExposesItsExponentPairs()
    {
        var force = Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

        var pairs = force.GetSnapshot().Pairs;

        pairs.Should().HaveCount(3);
        pairs[FundamentalDimension.Mass].Should().Be(1);
        pairs[FundamentalDimension.Length].Should().Be(1);
        pairs[FundamentalDimension.Time].Should().Be(-2);
    }

    [Fact]
    public void DimensionalityStateIsOrderedCanonically()
    {
        // Not a format decision — a promise to whoever writes the pairs out, so they get a stable result for
        // dimensionally-equal values without having to sort them.
        var oneWay = Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);
        var otherWay = Dimensionality.Length / Dimensionality.Time * Dimensionality.Mass / Dimensionality.Time;

        oneWay.GetSnapshot().Pairs.Keys.Should().Equal(otherWay.GetSnapshot().Pairs.Keys);
        oneWay.GetSnapshot().Pairs.Keys.Should().Equal(
            FundamentalDimension.Mass, FundamentalDimension.Length, FundamentalDimension.Time);
    }

    [Fact]
    public void DimensionlessStateIsEmptyAndRoundTrips()
    {
        Dimensionality.Dimensionless.GetSnapshot().Pairs.Should().BeEmpty();
        Dimensionality.FromSnapshot(default).Should().Be(Dimensionality.Dimensionless);

        // default(Dimensionality) has no backing map at all; it must behave the same way.
        default(Dimensionality).GetSnapshot().Pairs.Should().BeEmpty();
    }

    [Fact]
    public void DimensionalityRoundTripsThroughItsState()
    {
        var dimension = Dimensionality.Mass * Dimensionality.Length * Dimensionality.Length
                        / (Dimensionality.Time * Dimensionality.Time * Dimensionality.Temperature);

        Dimensionality.FromSnapshot(dimension.GetSnapshot()).Should().Be(dimension);
    }

    [Fact]
    public void DimensionalityStateEqualityIsStructural()
    {
        // The compiler-generated Equals would compare dictionary references, and that would propagate into
        // QuantitySnapshot and MeasurandSnapshot, whose equality is built from their fields'.
        var force = Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);
        var sameAgain = Dimensionality.Length * Dimensionality.Mass / (Dimensionality.Time * Dimensionality.Time);

        force.GetSnapshot().Should().Be(sameAgain.GetSnapshot());
        force.GetSnapshot().GetHashCode().Should().Be(sameAgain.GetSnapshot().GetHashCode());
        force.GetSnapshot().Should().NotBe(Dimensionality.Mass.GetSnapshot());
    }

    [Fact]
    public void MeasurandRoundTripsValueAndUncertaintyTogether()
    {
        var original = Mass.Kilogram.Quantity(2).Measurand(AsymmetricUncertainty.FromRelative(0.05, 0.01));

        var rebuilt = Measurand.FromSnapshot(original.GetSnapshot());

        rebuilt.In(Mass.Kilogram).Should().Be(2);
        rebuilt.UpperRelativeUncertainty.Should().Be(0.05);
        rebuilt.LowerRelativeUncertainty.Should().Be(0.01);
    }
}
