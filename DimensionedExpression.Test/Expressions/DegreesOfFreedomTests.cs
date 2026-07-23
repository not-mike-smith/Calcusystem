using DimensionedExpression.Expressions;
using FluentAssertions;
using Measurement;
using Measurement.Models;
using Measurement.Uncertainty;
using Measurement.Units;
using Xunit;

namespace DimensionedExpression.Test.Expressions;

public class DegreesOfFreedomTests
{
    private static readonly Dimensionality Mass = Measurement.Units.Mass.Kilogram.Dimensionality;
    private static readonly Dimensionality Length = Measurement.Units.Length.Meter.Dimensionality;

    private static Variable Unbound(Dimensionality dim) =>
        new("x", dim);

    private static Variable Bound(double kgValue) =>
        new("x", Measurement.Units.Mass.Kilogram.Quantity(kgValue).Measurand(GaussianUncertainty.FromRelErr(0)));

    [Fact]
    public void UnboundDirectVariable_HasOneDoF()
    {
        Unbound(Mass).DegreesOfFreedom().Should().Be(1);
    }

    [Fact]
    public void BoundDirectVariable_HasZeroDoF()
    {
        Bound(5).DegreesOfFreedom().Should().Be(0);
    }

    [Fact]
    public void NegatedUnbound_PropagatesDoF()
    {
        new NegatedVariable(Unbound(Mass)).DegreesOfFreedom().Should().Be(1);
    }

    [Fact]
    public void NegatedBound_HasZeroDoF()
    {
        new NegatedVariable(Bound(5)).DegreesOfFreedom().Should().Be(0);
    }

    [Fact]
    public void ReciprocalUnbound_PropagatesDoF()
    {
        new ReciprocalExpression(Unbound(Mass)).DegreesOfFreedom().Should().Be(1);
    }

    [Fact]
    public void ReciprocalBound_HasZeroDoF()
    {
        new ReciprocalExpression(Bound(5)).DegreesOfFreedom().Should().Be(0);
    }

    [Fact]
    public void ProductExpression_TwoUnboundFactors_HasTwoDoF()
    {
        var product = new ProductExpression();
        product.AddFactor(Unbound(Mass));
        product.AddFactor(Unbound(Length));
        product.DegreesOfFreedom().Should().Be(2);
    }

    [Fact]
    public void ProductExpression_AllBound_HasZeroDoF()
    {
        var product = new ProductExpression();
        product.AddFactor(Bound(5));
        product.AddFactor(Bound(3));
        product.DegreesOfFreedom().Should().Be(0);
    }

    [Fact]
    public void ProductExpression_MixedBoundedness_CountsOnlyUnbound()
    {
        var product = new ProductExpression();
        product.AddFactor(Bound(5));
        product.AddFactor(Unbound(Length));
        product.DegreesOfFreedom().Should().Be(1);
    }

    [Fact]
    public void SumExpression_TwoUnbound_HasTwoDoF()
    {
        var sum = new SumExpression(Mass);
        sum.AddAddend(Unbound(Mass));
        sum.AddAddend(Unbound(Mass));
        sum.DegreesOfFreedom().Should().Be(2);
    }

    [Fact]
    public void SumExpression_OneBoundOneUnbound_HasOneDoF()
    {
        var sum = new SumExpression(Mass);
        sum.AddAddend(Bound(3));
        sum.AddAddend(Unbound(Mass));
        sum.DegreesOfFreedom().Should().Be(1);
    }

    [Fact]
    public void QuotientExpression_UnboundNumerator_BoundDenominator_HasOneDoF()
    {
        var quotient = new QuotientExpression
        {
            Id = "test",
            Numerator = Unbound(Mass),
            Denominator = Bound(2)
        };
        quotient.DegreesOfFreedom().Should().Be(1);
    }

    [Fact]
    public void QuotientExpression_BothBound_HasZeroDoF()
    {
        var quotient = new QuotientExpression
        {
            Id = "test",
            Numerator = Bound(10),
            Denominator = Bound(2)
        };
        quotient.DegreesOfFreedom().Should().Be(0);
    }

    [Fact]
    public void QuotientExpression_BothUnbound_HasTwoDoF()
    {
        var quotient = new QuotientExpression
        {
            Id = "test",
            Numerator = Unbound(Mass),
            Denominator = Unbound(Mass)
        };
        quotient.DegreesOfFreedom().Should().Be(2);
    }

    [Fact]
    public void NestedProductExpression_RecursivelyCountsAllUnbound()
    {
        // (a * b) * c → 3 DoF
        var inner = new ProductExpression();
        inner.AddFactor(Unbound(Mass));
        inner.AddFactor(Unbound(Mass));

        var outer = new ProductExpression();
        outer.AddFactor(inner);
        outer.AddFactor(Unbound(Mass));

        outer.DegreesOfFreedom().Should().Be(3);
    }

    [Fact]
    public void NestedExpression_PartiallyBound_CountsCorrectly()
    {
        // (a * 5kg) / b, where a is unbound → 2 DoF
        var numerator = new ProductExpression();
        numerator.AddFactor(Unbound(Mass));
        numerator.AddFactor(Bound(5));

        var quotient = new QuotientExpression
        {
            Id = "test",
            Numerator = numerator,
            Denominator = Unbound(Mass)
        };

        quotient.DegreesOfFreedom().Should().Be(2);
    }
}
