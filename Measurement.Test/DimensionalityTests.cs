using System;
using System.Collections.Generic;
using System.Linq;
using Measurement.Exceptions;
using FluentAssertions;
using Measurement.Models;
using Xunit;

namespace Measurement.Test;

public class DimensionalityTests
{
    [Fact]
    public void DefaultIsDimensionless()
    {
        Dimensionality d = default;
        d.Should().BeEquivalentTo(Dimensionality.Dimensionless);
        (d == Dimensionality.Dimensionless).Should().BeTrue();
        d[FundamentalDimension.Mass].Should().Be(0);
    }

    [Fact]
    public void BracketAccessorWorks()
    {
        Dimensionality.Mass[FundamentalDimension.Mass].Should().Be(1);
        Dimensionality.Mass[FundamentalDimension.Currency].Should().Be(0);
        Dimensionality.Mass[FundamentalDimension.AmountOfMatter].Should().Be(0);
        Dimensionality.Mass[FundamentalDimension.LuminousIntensity].Should().Be(0);
        Dimensionality.Mass[FundamentalDimension.ElectricCurrent].Should().Be(0);
        Dimensionality.Mass[FundamentalDimension.Length].Should().Be(0);
        Dimensionality.Mass[FundamentalDimension.Temperature].Should().Be(0);
        Dimensionality.Mass[FundamentalDimension.Angle].Should().Be(0);
        Dimensionality.Mass[FundamentalDimension.Time].Should().Be(0);
    }

    [Fact]
    public void GetHashCodeIsGoodHashingFunction()
    {
        var hugeList = HugeListOfDimensionalities;
        var uniqueHashes = hugeList.Select(d => d.GetHashCode()).ToHashSet();
        uniqueHashes.Count.Should().Be(hugeList.Count);
    }

    [Fact(/**/ Skip = "takes a while b/c is O(n^2) on a long list" /**/)]
    public void DifferentDimensionsAreNotEqual()
    {
        var hugeList = HugeListOfDimensionalities;
        for (var i = 1; i < hugeList.Count; i++)
        {
            for (var j = 0; j < i; j++)
            {
                var di = hugeList[i];
                var dj = hugeList[j];
                di.Equals(dj).Should().BeFalse(); // ($"{i}:{di.ToString()}::{j}:{dj.ToString()}");
                (di == dj).Should().BeFalse();
                (di != dj).Should().BeTrue();
            }
        }
    }

    [Fact]
    public void SameDimensionsAreEqual()
    {
        foreach (var d in HugeListOfDimensionalities)
        {
            d.Equals(d).Should().BeTrue();
            (d == d).Should().BeTrue();
            (d != d).Should().BeFalse();
        }
    }

    [Fact]
    public void ToStringIsUnique()
    {
        var hugeList = HugeListOfDimensionalities;
        var uniqueStrings = hugeList.Select(d => d.ToString()).ToHashSet();
        uniqueStrings.Count.Should().Be(hugeList.Count);
    }

    [Fact]
    public void MultiplicationOverload_Dimensionality()
    {
        var area = Dimensionality.Length * Dimensionality.Length;
        area[FundamentalDimension.Length].Should().Be(2);
        area[FundamentalDimension.Mass].Should().Be(0);
        area[FundamentalDimension.Currency].Should().Be(0);
        area[FundamentalDimension.AmountOfMatter].Should().Be(0);
        area[FundamentalDimension.LuminousIntensity].Should().Be(0);
        area[FundamentalDimension.ElectricCurrent].Should().Be(0);
        area[FundamentalDimension.Temperature].Should().Be(0);
        area[FundamentalDimension.Angle].Should().Be(0);
        area[FundamentalDimension.Time].Should().Be(0);

        var volume = area * Dimensionality.Length;
        volume[FundamentalDimension.Length].Should().Be(3);

        var moment = Dimensionality.Mass * Dimensionality.Length;
        moment[FundamentalDimension.Length].Should().Be(1);
        moment[FundamentalDimension.Mass].Should().Be(1);
        moment[FundamentalDimension.Currency].Should().Be(0);
        moment[FundamentalDimension.AmountOfMatter].Should().Be(0);
        moment[FundamentalDimension.LuminousIntensity].Should().Be(0);
        moment[FundamentalDimension.ElectricCurrent].Should().Be(0);
        moment[FundamentalDimension.Temperature].Should().Be(0);
        moment[FundamentalDimension.Angle].Should().Be(0);
        moment[FundamentalDimension.Time].Should().Be(0);
    }

    [Fact]
    public void MultiplicationOverload_int()
    {
        var area = Dimensionality.Length * 2;
        area[FundamentalDimension.Length].Should().Be(2);
        area[FundamentalDimension.Mass].Should().Be(0);
        area[FundamentalDimension.Currency].Should().Be(0);
        area[FundamentalDimension.AmountOfMatter].Should().Be(0);
        area[FundamentalDimension.LuminousIntensity].Should().Be(0);
        area[FundamentalDimension.ElectricCurrent].Should().Be(0);
        area[FundamentalDimension.Temperature].Should().Be(0);
        area[FundamentalDimension.Angle].Should().Be(0);
        area[FundamentalDimension.Time].Should().Be(0);

        var volume = Dimensionality.Length * 3;
        volume[FundamentalDimension.Length].Should().Be(3);
    }

    [Fact]
    public void DivisionOverload_Dimensionality()
    {
        var frequency = Dimensionality.Dimensionless / Dimensionality.Time;
        frequency[FundamentalDimension.Length].Should().Be(0);
        frequency[FundamentalDimension.Mass].Should().Be(0);
        frequency[FundamentalDimension.Currency].Should().Be(0);
        frequency[FundamentalDimension.AmountOfMatter].Should().Be(0);
        frequency[FundamentalDimension.LuminousIntensity].Should().Be(0);
        frequency[FundamentalDimension.ElectricCurrent].Should().Be(0);
        frequency[FundamentalDimension.Temperature].Should().Be(0);
        frequency[FundamentalDimension.Angle].Should().Be(0);
        frequency[FundamentalDimension.Time].Should().Be(-1);

        var velocity = Dimensionality.Length / Dimensionality.Time;
        velocity[FundamentalDimension.Length].Should().Be(1);
        velocity[FundamentalDimension.Mass].Should().Be(0);
        velocity[FundamentalDimension.Currency].Should().Be(0);
        velocity[FundamentalDimension.AmountOfMatter].Should().Be(0);
        velocity[FundamentalDimension.LuminousIntensity].Should().Be(0);
        velocity[FundamentalDimension.ElectricCurrent].Should().Be(0);
        velocity[FundamentalDimension.Temperature].Should().Be(0);
        velocity[FundamentalDimension.Angle].Should().Be(0);
        velocity[FundamentalDimension.Time].Should().Be(-1);

        var acceleration = velocity / Dimensionality.Time;
        acceleration[FundamentalDimension.Length].Should().Be(1);
        acceleration[FundamentalDimension.Mass].Should().Be(0);
        acceleration[FundamentalDimension.Currency].Should().Be(0);
        acceleration[FundamentalDimension.AmountOfMatter].Should().Be(0);
        acceleration[FundamentalDimension.LuminousIntensity].Should().Be(0);
        acceleration[FundamentalDimension.ElectricCurrent].Should().Be(0);
        acceleration[FundamentalDimension.Temperature].Should().Be(0);
        acceleration[FundamentalDimension.Angle].Should().Be(0);
        acceleration[FundamentalDimension.Time].Should().Be(-2);
    }

    [Fact]
    public void DivisionOverload_int_ThrowsOnNonIntegerExponent()
    {
        var energy = Dimensionality.Mass * (Dimensionality.Length * 2) / (Dimensionality.Time * 2);
        Func<Dimensionality> shouldThrow = () => energy / 2;
        shouldThrow.Should().Throw<NondiscreteDimensionalityException>();
    }

    [Fact]
    public void DivisionOverload_int_ThrowsOnZerothRoot()
    {
        var volume = Dimensionality.Length * 3;
        Func<Dimensionality> shouldThrow = () => volume / 0;
        shouldThrow.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void DivisionOverload_int_ThrowsOnNegativeRoot()
    {
        var volume = Dimensionality.Length * 3;
        Func<Dimensionality> shouldThrow = () => volume / -1;
        shouldThrow.Should().Throw<NondiscreteDimensionalityException>();
    }

    [Fact]
    public void DivisionOverload_int()
    {
        var specificEnergy = (Dimensionality.Length * 2) / (Dimensionality.Time * 2);
        var velocity = specificEnergy / 2;
        velocity[FundamentalDimension.Length].Should().Be(1);
        velocity[FundamentalDimension.Mass].Should().Be(0);
        velocity[FundamentalDimension.Currency].Should().Be(0);
        velocity[FundamentalDimension.AmountOfMatter].Should().Be(0);
        velocity[FundamentalDimension.LuminousIntensity].Should().Be(0);
        velocity[FundamentalDimension.ElectricCurrent].Should().Be(0);
        velocity[FundamentalDimension.Temperature].Should().Be(0);
        velocity[FundamentalDimension.Angle].Should().Be(0);
        velocity[FundamentalDimension.Time].Should().Be(-1);

        var volume = Dimensionality.Length * 3;
        var length = volume / 3;
        length[FundamentalDimension.Length].Should().Be(1);
        length[FundamentalDimension.Mass].Should().Be(0);
        length[FundamentalDimension.Currency].Should().Be(0);
        length[FundamentalDimension.AmountOfMatter].Should().Be(0);
        length[FundamentalDimension.LuminousIntensity].Should().Be(0);
        length[FundamentalDimension.ElectricCurrent].Should().Be(0);
        length[FundamentalDimension.Temperature].Should().Be(0);
        length[FundamentalDimension.Angle].Should().Be(0);
        length[FundamentalDimension.Time].Should().Be(0);
    }

    [Fact]
    public void MoreMathOverloadTesting()
    {
        var acceleration = Dimensionality.Length / (Dimensionality.Time * 2);
        var force = Dimensionality.Mass * acceleration;
        var pressure = force / (Dimensionality.Length * 2);
        var volume = Dimensionality.Length * 3;
        var energy = pressure * volume;
        var specificEnergy = energy / Dimensionality.Mass;
        var velocity = specificEnergy / 2;
    }

    #region Huge List

    public static IReadOnlyList<Dimensionality> HugeListOfDimensionalities => LazyListOfDimensionalities.Value;

    private static Lazy<IReadOnlyList<Dimensionality>> LazyListOfDimensionalities =
        new Lazy<IReadOnlyList<Dimensionality>>(GetHugeListOfDimensionalities);
    private static IReadOnlyList<Dimensionality> GetHugeListOfDimensionalities()
    {
        var x = new List<Dimensionality>
        {
            Dimensionality.Currency,
            Dimensionality.AmountOfMatter,
            Dimensionality.Mass,
            Dimensionality.LuminousIntensity,
            Dimensionality.ElectricCurrent,
            Dimensionality.Length,
            Dimensionality.Temperature,
            Dimensionality.Angle,
            Dimensionality.Time,
        };

        var count = x.Count;

        for (var i = 0; i < count; i++)
        {
            var di = x[i];
            x.Add(di * di);
            x.Add(Dimensionality.Dimensionless / di);
            x.Add(Dimensionality.Dimensionless / di / di);
        }
        for (var i = 1; i < count; i++)
        {
            for (var j = 0; j < i; j++)
            {
                var di = x[i];
                var dj = x[j];
                x.Add(di * dj);
                x.Add(di * di * dj);
                x.Add(di * dj * dj);
                x.Add(di / dj);
                x.Add(di * di / dj);
                x.Add(di / dj / dj);
                x.Add(di * di / dj / dj);
                x.Add(dj / di);
                x.Add(dj * dj / di);
                x.Add(dj / di / di);
                x.Add(dj * dj / di / di);
            }
        }
        for (var i = 2; i < count; i++)
        {
            for (var j = 1; j < i; j++)
            {
                for (var k = 0; k < j; k++)
                {
                    var di = x[i];
                    var dj = x[j];
                    var dk = x[k];
                    x.Add(di * dj * dk);
                    x.Add(Dimensionality.Dimensionless / di / dj / dk);
                    x.Add(di * di * dj * dj * dk * dk);
                    x.Add(Dimensionality.Dimensionless / di / di / dj / dj / dk / dk);
                    x.AddRange(PermutationsOf3(di, dj , dk));
                }
            }
        }

        x.Add(Dimensionality.Dimensionless);

        return x;
    }

    private static List<Dimensionality> PermutationsOf3(Dimensionality di, Dimensionality dj, Dimensionality dk)
    {
        var ret = new List<Dimensionality>();
        ret.AddRange(MutationsOf3(di, dj, dk));
        ret.AddRange(MutationsOf3(dj, dk, di));
        ret.AddRange(MutationsOf3(dk, di, dj));
        return ret;
    }

    private static List<Dimensionality> MutationsOf3(Dimensionality di, Dimensionality dj, Dimensionality dk)
    {
        return new List<Dimensionality>
        {
            di * di * dj * dk,
            di * dj * dj * dk * dk,
            Dimensionality.Dimensionless / di / di / dj / dk,
            Dimensionality.Dimensionless / di / dj / dj / dk / dk,

            di * dj / dk,
            di * di * dj / dk,
            di * dj * dj / dk,
            di * di * dj * dj / dk,
            di * dj / dk / dk,
            di * di * dj / dk / dk,
            di * dj * dj / dk / dk,
            di * di * dj * dj / dk / dk,

            di / dj / dk,
            di * di / dj / dk,
            di / dj / dj / dk,
            di * di / dj / dj / dk,
            di / dj / dk / dk,
            di * di / dj / dk / dk,
            di / dj / dj / dk / dk,
            di * di / dj / dj / dk / dk,
        };
    }

    #endregion
}
