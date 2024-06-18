using System;
using FluentAssertions;
using Measurement.Models;
using Xunit;

namespace Measurement.Test;

public class OffsetUnitOfMeasureTests
{
    private static readonly UnitOfMeasure Kelvin = new UnitOfMeasure(Dimensionality.Temperature, "K", 1);

    [Fact]
    public void HappyPath()
    {
        var fahrenheit = new OffsetUnitOfMeasure(
            Dimensionality.Temperature,
            "F",
            5d / 9,
            459.67);

        fahrenheit.Dimensionality.Should().Be(Dimensionality.Temperature);
        fahrenheit.Symbol.Should().Be("F");
        fahrenheit.KmsConversionFactor.Should().BeApproximately(5d / 9, 1E-9);
        fahrenheit.ConvertFromKmsValue(273.15).Should().BeApproximately(32, 1E-9);
        fahrenheit.ConvertToKmsValue(212).Should().BeApproximately(373.15, 1E-9);
    }

    [Fact]
    public void ThrowsOnNaNOffset()
    {
        Func<OffsetUnitOfMeasure> shouldThrow = () => new OffsetUnitOfMeasure(
            Dimensionality.Temperature,
            "C",
            1,
            double.NaN);

        shouldThrow.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void ThrowsOnInfiniteOffset(double infinity)
    {
        Func<OffsetUnitOfMeasure> shouldThrow = () => new OffsetUnitOfMeasure(
            Dimensionality.Time,
            "eon",
            1E12,
            infinity);

        shouldThrow.Should().Throw<ArgumentException>();
    }
}
