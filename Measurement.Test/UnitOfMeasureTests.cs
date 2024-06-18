using System;
using Measurement.Units;
using FluentAssertions;
using Measurement.Models;
using Xunit;

namespace Measurement.Test;

public class UnitOfMeasureTests
{
    [Fact]
    public void HappyPath()
    {
        var dozen = new UnitOfMeasure(Dimensionality.Dimensionless, "dz", 12);
        dozen.Dimensionality.Should().Be(Dimensionality.Dimensionless);
        dozen.Symbol.Should().Be("dz");
        dozen.KmsConversionFactor.Should().Be(12);
        dozen.ConvertFromKmsValue(36).Should().Be(3);
        dozen.ConvertToKmsValue(2).Should().Be(24);
    }

    [Fact]
    public void CannotHaveZeroConversionFactor()
    {
        Func<UnitOfMeasure> shouldThrow = () => new UnitOfMeasure(Dimensionality.Dimensionless, "0", 0);
        shouldThrow.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void CannotHaveNegativeConversionFactor()
    {
        Func<UnitOfMeasure> shouldThrow = () => new UnitOfMeasure(Dimensionality.Dimensionless, "negative", -1);
        shouldThrow.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CannotHaveNaNConversionFactor()
    {
        Func<UnitOfMeasure> shouldThrow = () => new UnitOfMeasure(Dimensionality.Dimensionless, "nan", double.NaN);
        shouldThrow.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void CannotHaveInfiniteConversionFactor(double infinity)
    {
        Func<UnitOfMeasure> shouldThrow = () => new UnitOfMeasure(Dimensionality.Dimensionless, "infinity", infinity);
        shouldThrow.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CannotHaveNullSymbol()
    {
        Func<UnitOfMeasure> shouldThrow = () => new UnitOfMeasure(
            Dimensionality.Dimensionless,
            null,
            1);

        shouldThrow.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EddyCorrectionFactor()
    {
        Dimensionless.EddyCorrectionFactor.KmsConversionFactor.Should().NotBe(1);
    }

    [Fact]
    public static void DerivedUnitString()
    {
        var kgPerM3 = Density.KgPerM3.ToString();
        kgPerM3.Should().Be("kg/m³");
        Density.GramPerCc.ToString().Should().Be("g/cm³");
    }
}
