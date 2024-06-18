using System.Linq;
using FluentAssertions;
using Measurement.Models;
using Xunit;

namespace Measurement.Test;

public class FundamentalDimensionTests
{
    [Fact]
    public void HashCodesAreUnique()
    {
        var x = FundamentalDimension.All.Select(f => f.GetHashCode()).ToHashSet();
        x.Count.Should().Be(FundamentalDimension.All.Count);
    }

    [Fact]
    public void OrderShouldBeCorrect()
    {
        var x = FundamentalDimension.All.Reverse().OrderBy(f => f).ToList();
        x[0].Should().Be(FundamentalDimension.Currency);
        x[1].Should().Be(FundamentalDimension.AmountOfMatter);
        x[2].Should().Be(FundamentalDimension.Mass);
        x[3].Should().Be(FundamentalDimension.LuminousIntensity);
        x[4].Should().Be(FundamentalDimension.ElectricCurrent);
        x[5].Should().Be(FundamentalDimension.Length);
        x[6].Should().Be(FundamentalDimension.Temperature);
        x[7].Should().Be(FundamentalDimension.Angle);
        x[8].Should().Be(FundamentalDimension.Time);
    }
}
