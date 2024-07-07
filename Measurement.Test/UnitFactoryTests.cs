using System;
using FluentAssertions;
using Measurement.Factories;
using Measurement.Models;
using Xunit;

namespace Measurement.Test;

public class UnitFactoryTests
{
    private static readonly UnitOfMeasure Dozen = new UnitOfMeasure(Dimensionality.Dimensionless, "dz", 12);
    private static readonly UnitOfMeasure Centimeter = new UnitOfMeasure(Dimensionality.Length, "cm", 0.01);
    private static readonly UnitOfMeasure Minute = new UnitOfMeasure(Dimensionality.Time, "min", 60);
    private static readonly UnitOfMeasure Meter = new UnitOfMeasure(Dimensionality.Length, "m", 1);
    private static readonly UnitOfMeasure Kilogram = new UnitOfMeasure(Dimensionality.Mass, "kg", 1);
    private static readonly UnitOfMeasure Second = new UnitOfMeasure(Dimensionality.Time, "s", 1);
    private static readonly UnitOfMeasure Kelvin = new UnitOfMeasure(Dimensionality.Temperature, "K", 1);

    [Fact]
    public void SimpleCreation()
    {
        var dozen = UnitFactory.Create("dz", Dimensionality.Dimensionless, 12);
        dozen.Dimensionality.Should().Be(Dimensionality.Dimensionless);
        dozen.Symbol.Should().Be("dz");
        dozen.KmsConversionFactor.Should().Be(12);
        dozen.ConvertFromKmsValue(36).Should().Be(3);
        dozen.ConvertToKmsValue(2).Should().Be(24);
    }

    [Fact]
    public void CreateByScalingOtherUnit_HappyPath()
    {
        var gross = UnitFactory.Create("Gr", 12, Dozen);
        gross.Dimensionality.Should().Be(Dimensionality.Dimensionless);
        gross.Symbol.Should().Be("Gr");
        gross.KmsConversionFactor.Should().Be(144);
        gross.ConvertFromKmsValue(216).Should().Be(1.5);
        gross.ConvertToKmsValue(2).Should().Be(288);
    }

    [Fact]
    public void CreateByScalingOtherUnit_ThrowsOnZeroScale()
    {
        Func<UnitOfMeasure> shouldThrow = () => UnitFactory.Create("zen", 0, Dozen);
        shouldThrow.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void CreateByScalingOtherUnit_ThrowsOnNegativeScale()
    {
        Func<UnitOfMeasure> shouldThrow = () => UnitFactory.Create("-2", -2, Dozen);
        shouldThrow.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateByScalingOtherUnit_ThrowsOnNullUnit()
    {
        Func<UnitOfMeasure> shouldThrow = () => UnitFactory.Create("2", 2, unitOfMeasure: null!);
        shouldThrow.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateByConstituentUnitsWithSymbol_HappyPath()
    {
        var cc = UnitFactory.Create("cc", (Centimeter, 3));
        cc.Dimensionality.Should().Be(Dimensionality.Length * 3);
        cc.Symbol.Should().Be("cc");
        cc.KmsConversionFactor.Should().BeApproximately(0.000001, 1E-9);
        cc.ConvertFromKmsValue(1).Should().BeApproximately(1000000, 1E-9);
        cc.ConvertToKmsValue(2000).Should().BeApproximately(0.002, 1E-9);

        var cmPerMin = UnitFactory.Create("cm/min", (Centimeter, 1), (Minute, -1));
        cmPerMin.Dimensionality.Should().Be(Dimensionality.Length / Dimensionality.Time);
        cmPerMin.KmsConversionFactor.Should().BeApproximately(0.01 / 60, 1E-9);
        cmPerMin.ConvertFromKmsValue(1).Should().BeApproximately(6000, 1E-9);
        cmPerMin.ConvertToKmsValue(300).Should().BeApproximately(0.05, 1E-9);
    }

    [Fact]
    public void CreateByConstituentUnitsWithSymbol_ThrowsOnNullSymbol()
    {
        Func<UnitOfMeasure> shouldThrow = () => UnitFactory.Create(null!, (Centimeter, 3));
        shouldThrow.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateByConstituentUnitsWithoutSymbol_HappyPath()
    {
        var cm2 = UnitFactory.Create((Centimeter, 2));
        cm2.Dimensionality.Should().Be(Dimensionality.Length * 2);
        cm2.Symbol.Should().Be("cm²");
        cm2.KmsConversionFactor.Should().BeApproximately(0.0001, 1E-9);

        var joule = UnitFactory.Create(
            (Kilogram, 1),
            (Second, -2),
            (Meter, 2));

        joule.Symbol.Should().Be("kg·m²/s²");
    }

    [Fact]
    public void CreateOffsetUnitOfMeasure_HappyPath()
    {
        var celsius = UnitFactory.Create("C", 1, Kelvin, 273.15);
        celsius.Dimensionality.Should().Be(Dimensionality.Temperature);
        celsius.Symbol.Should().Be("C");
        celsius.KmsConversionFactor.Should().Be(1);
        celsius.ConvertFromKmsValue(274.15).Should().BeApproximately(1, 1E-9);
        celsius.ConvertToKmsValue(100).Should().BeApproximately(373.15, 1E-9);
        var fahrenheit = UnitFactory.Create("R", 5d/9, Kelvin, 459.67);
        fahrenheit.ConvertFromKmsValue(373.15).Should().BeApproximately(212, 1E-9);
    }

    [Fact]
    public void CreateSiPrefixedUnits_HappyPath()
    {
        var km = UnitFactory.Create(Metric.Kilo, Meter);
        km.Dimensionality.Should().Be(Dimensionality.Length);
        km.Symbol.Should().Be("km");
        km.KmsConversionFactor.Should().Be(1000);

        var micrometer1 = UnitFactory.Create(Metric.Micro, Meter);
        micrometer1.Symbol.Should().Be("μm");
        micrometer1.KmsConversionFactor.Should().Be(1E-6);

    }
}
