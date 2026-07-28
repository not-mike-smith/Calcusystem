using System;
using FluentAssertions;
using Measurement.Models;
using Measurement.Uncertainty;
using Measurement.Units;
using Xunit;

namespace Measurement.Test;

public class MeasurandTests
{
    private static Measurand Meters(double value, double relativeError = 0) =>
        Length.Meter.Quantity(value).Measurand(GaussianUncertainty.FromRelErr(relativeError));

    private static Measurand Kilograms(double value, double relativeError = 0) =>
        Mass.Kilogram.Quantity(value).Measurand(GaussianUncertainty.FromRelErr(relativeError));

    [Fact]
    public void TryAdd_HappyPath()
    {
        var a = Meters(1, 0.03);
        var b = Meters(2, 0.04);
        var sum = a.TryAdd(b);
        sum.Dimensionality.Should().Be(Dimensionality.Length);
        sum.KmsValue.Should().BeApproximately(3, 1E-9);
    }

    [Fact]
    public void TryAdd_NaNOnMismatch()
    {
        var length = Meters(1);
        var mass = Kilograms(1);
        var result = length.TryAdd(mass);
        result.IsNaN().Should().BeTrue();
    }

    [Fact]
    public void TrySubtract_HappyPath()
    {
        var a = Meters(3, 0.03);
        var b = Meters(1, 0.04);
        var diff = a.TrySubtract(b);
        diff.Dimensionality.Should().Be(Dimensionality.Length);
        diff.KmsValue.Should().BeApproximately(2, 1E-9);
    }

    [Fact]
    public void TrySubtract_NaNOnMismatch()
    {
        var length = Meters(1);
        var mass = Kilograms(1);
        var result = length.TrySubtract(mass);
        result.IsNaN().Should().BeTrue();
    }
}
