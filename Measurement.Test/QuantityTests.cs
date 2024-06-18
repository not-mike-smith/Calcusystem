using System;
using Measurement.Exceptions;
using FluentAssertions;
using Measurement.Models;
using Xunit;

namespace Measurement.Test;

public class QuantityTests
{
    private static readonly UnitOfMeasure Unity = new UnitOfMeasure(Dimensionality.Dimensionless, "1", 1);
    private static readonly UnitOfMeasure Percent = new UnitOfMeasure(Dimensionality.Dimensionless, "%", 0.01);
    private static readonly UnitOfMeasure Seconds = new UnitOfMeasure(Dimensionality.Time, "s", 1);

    private static readonly Dimensionality Volumetric = Dimensionality.Length * 3;
    private static readonly Dimensionality Pressure = Dimensionality.Mass / Dimensionality.Length / (Dimensionality.Time * 2);
    private static readonly Dimensionality Energy = Dimensionality.Mass * (Dimensionality.Length * 2) / (Dimensionality.Time * 2);
    private static readonly UnitOfMeasure Joule = new UnitOfMeasure(Energy, "J", 1);
    private static readonly UnitOfMeasure MPa = new UnitOfMeasure(Pressure, "MPa", 1E6);
    private static readonly UnitOfMeasure Liter = new UnitOfMeasure(Volumetric, "L", 0.001);

    private static readonly UnitOfMeasure Meter = new UnitOfMeasure(Dimensionality.Length, "m", 1);

    [Fact]
    public void DefaultIsDimensionlessNaN()
    {
        Quantity d = default;
        d.Dimensionality.Should().Be(Dimensionality.Dimensionless);
        double.IsNaN(d.In(Unity)).Should().BeTrue();
    }

    [Fact]
    public void Conversion_HappyPath()
    {
        var q = new Quantity(110, Percent);
        q.In(Unity).Should().Be(1.1);
    }

    [Fact]
    public void Conversion_ThrowsOnDimensionsMismatch()
    {
        var q = new Quantity(30, Seconds);
        Func<double> shouldThrow = () => q.In(Percent);
        shouldThrow.Should().Throw<IncompatibleDimensionsException>();
    }

    [Fact]
    public void TryConversion_HappyPath()
    {
        var q = new Quantity(110, Percent);
        q.TryIn(Unity).Should().Be(1.1);
    }

    [Fact]
    public void TryConversion_NaNOnDimensionMismatch()
    {
        var q = new Quantity(30, Seconds);
        double.IsNaN(q.TryIn(Percent)).Should().BeTrue();
    }

    [Fact]
    public void PlusOverload_HappyPath()
    {
        var q1 = new Quantity(500, Percent);
        var q2 = new Quantity(3.25, Unity);
        var q3 = q1 + q2;
        q3.Dimensionality.Should().Be(Dimensionality.Dimensionless);
        q3.In(Percent).Should().Be(825);

        var q4 = new Quantity(5.3, Seconds) + new Quantity(4.7, Seconds);
        q4.Dimensionality.Should().Be(Dimensionality.Time);
        q4.In(Seconds).Should().BeApproximately(10, 1E-9);
    }

    [Fact]
    public void PlusOverload_ThrowsOnDimensionsMismatch()
    {
        var q1 = new Quantity(5, Seconds);
        var q2 = new Quantity(30, Percent);
        Func<Quantity> shouldThrow = () => q1 + q2;
        shouldThrow.Should().Throw<IncompatibleDimensionsException>();
    }

    [Fact]
    public void MinusOverload_HappyPath()
    {
        var q1 = new Quantity(500, Percent);
        var q2 = new Quantity(3.25, Unity);
        var q3 = q1 - q2;
        q3.Dimensionality.Should().Be(Dimensionality.Dimensionless);
        q3.In(Percent).Should().Be(175);

        var q4 = new Quantity(5.3, Seconds) - new Quantity(4.7, Seconds);
        q4.Dimensionality.Should().Be(Dimensionality.Time);
        q4.In(Seconds).Should().BeApproximately(0.6, 1E-9);
    }

    [Fact]
    public void MinusOverload_ThrowsOnDimensionsMismatch()
    {
        var q1 = new Quantity(5, Seconds);
        var q2 = new Quantity(30, Percent);
        Func<Quantity> shouldThrow = () => q1 - q2;
        shouldThrow.Should().Throw<IncompatibleDimensionsException>();
    }

    [Fact]
    public void TimesOverload_HappyPath()
    {
        var p = new Quantity(0.5, MPa);
        var v = new Quantity(3, Liter);
        var w = p * v;
        w.Dimensionality.Should().Be(Energy);
        w.In(Joule).Should().Be(1500);
    }

    [Fact]
    public void DivisionOverload_HappyPath()
    {
        var w = new Quantity(3000, Joule);
        var p = new Quantity(0.1, MPa);
        var v = w / p;
        v.Dimensionality.Should().Be(Volumetric);
        v.In(Liter).Should().Be(30);

        var n = v / new Quantity(3, Liter);
        n.Dimensionality.Should().Be(Dimensionality.Dimensionless);
        n.In(Unity).Should().Be(10);
    }

    [Fact]
    public void ExplicitCast()
    {
        var q = (Quantity) 7.05d;
        q.Dimensionality.Should().Be(Dimensionality.Dimensionless);
        q.In(Unity).Should().Be(7.05);
    }

    [Fact]
    public void ToPower()
    {
        var q = new Quantity(0.1, Meter);
        var v = q.ToPower(3);
        v.Dimensionality.Should().Be(Volumetric);
        v.In(Liter).Should().BeApproximately(1, 1E-9);
    }

    [Fact]
    public void ToRoot_HappyPath()
    {
        var v = new Quantity(8, Liter);
        var l = v.ToRoot(3);
        l.Dimensionality.Should().Be(Dimensionality.Length);
        l.In(Meter).Should().BeApproximately(0.2, 1E-9);
    }

    [Fact]
    public void ToRoot_NonIntegerExponents()
    {
        var w = new Quantity(3000, Joule);
        Func<Quantity> shouldThrow = () => w.ToRoot(2);
        shouldThrow.Should().Throw<NondiscreteDimensionalityException>();
    }

    [Fact]
    public void TryAdd_HappyPath()
    {
        var q1 = new Quantity(500, Percent);
        var q2 = new Quantity(3.25, Unity);
        var q3 = q1.TryAdd(q2);
        q3.Dimensionality.Should().Be(Dimensionality.Dimensionless);
        q3.In(Percent).Should().Be(825);

        var q4 = new Quantity(5.3, Seconds).TryAdd(new Quantity(4.7, Seconds));
        q4.Dimensionality.Should().Be(Dimensionality.Time);
        q4.In(Seconds).Should().BeApproximately(10, 1E-9);
    }

    [Fact]
    public void TryAdd_NaNOnMismatch()
    {
        var q1 = new Quantity(5, Seconds);
        var q2 = new Quantity(30, Percent);
        var q3 = q1.TryAdd(q2);
        q3.Dimensionality.Should().Be(q1.Dimensionality);
        q3.In(Seconds).Should().Be(double.NaN);
    }

    [Fact]
    public void TrySubtract_HappyPath()
    {
        var q1 = new Quantity(500, Percent);
        var q2 = new Quantity(3.25, Unity);
        var q3 = q1.TrySubtract(q2);
        q3.Dimensionality.Should().Be(Dimensionality.Dimensionless);
        q3.In(Percent).Should().Be(175);

        var q4 = new Quantity(5.3, Seconds).TrySubtract(new Quantity(4.7, Seconds));
        q4.Dimensionality.Should().Be(Dimensionality.Time);
        q4.In(Seconds).Should().BeApproximately(0.6, 1E-9);
    }

    [Fact]
    public void TrySubtract_NaNOnMismatch()
    {
        var q1 = new Quantity(5, Seconds);
        var q2 = new Quantity(30, Percent);
        var q3 = q1.TrySubtract(q2);
        q3.Dimensionality.Should().Be(q1.Dimensionality);
        q3.In(Seconds).Should().Be(double.NaN);
    }
}
