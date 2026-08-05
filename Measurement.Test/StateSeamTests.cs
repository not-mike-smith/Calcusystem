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
    public void QuantityRoundTripsWithStructuralDimensionality()
    {
        var original = new Quantity(9.81, Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time));

        var rebuilt = Quantity.FromState(original.GetState());

        rebuilt.In(Acceleration.MeterPerSecondSquared).Should().Be(9.81);
        rebuilt.Dimensionality.Should().Be(original.Dimensionality);
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
