using System.Linq;
using System;
using Calcusystem.Measurement.Dimensions;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Factories;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Quantities;
using Calcusystem.Measurement.Uncertainties;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Measurement.Test;

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
    public void DimensionalityStateExposesItsExponentPairs()
    {
        var force = Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

        var pairs = force.GetState().Pairs;

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

        oneWay.GetState().Pairs.Keys.Should().Equal(otherWay.GetState().Pairs.Keys);
        oneWay.GetState().Pairs.Keys.Should().Equal(
            FundamentalDimension.Mass, FundamentalDimension.Length, FundamentalDimension.Time);
    }

    [Fact]
    public void DimensionlessStateIsEmptyAndRoundTrips()
    {
        Dimensionality.Dimensionless.GetState().Pairs.Should().BeEmpty();
        Dimensionality.FromState(default).Should().Be(Dimensionality.Dimensionless);

        // default(Dimensionality) has no backing map at all; it must behave the same way.
        default(Dimensionality).GetState().Pairs.Should().BeEmpty();
    }

    [Fact]
    public void DimensionalityRoundTripsThroughItsState()
    {
        var dimension = Dimensionality.Mass * Dimensionality.Length * Dimensionality.Length
                        / (Dimensionality.Time * Dimensionality.Time * Dimensionality.Temperature);

        Dimensionality.FromState(dimension.GetState()).Should().Be(dimension);
    }

    [Fact]
    public void DimensionalityStateEqualityIsStructural()
    {
        // The compiler-generated Equals would compare dictionary references, and that would propagate into
        // QuantityState and MeasurandState, whose equality is built from their fields'.
        var force = Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);
        var sameAgain = Dimensionality.Length * Dimensionality.Mass / (Dimensionality.Time * Dimensionality.Time);

        force.GetState().Should().Be(sameAgain.GetState());
        force.GetState().GetHashCode().Should().Be(sameAgain.GetState().GetHashCode());
        force.GetState().Should().NotBe(Dimensionality.Mass.GetState());
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
