using System.Linq;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Units;
using Xunit;

namespace Calcusystem.Measurement.Test;

public class Sandbox
{
    [Fact]
    public void Play()
    {
        var x = Lists.UnitTypes;

        var oneKgPlusOrMinusAGram = Mass.Kilogram
            .Quantity(1)
            .WithUncertainty(0.1.Percent());

        oneKgPlusOrMinusAGram = Mass.Kilogram.Quantity(1).WithUncertainty(Mass.Gram.Quantity(1));

        var oneKg = Mass.Kilogram.Quantity(1);

        var oneKgPlusOrMinusAMilligram = oneKg.WithUncertainty(1.0.Units(Mass.Milligram));

        var oneKgPlusOrMinusALittleOrALot = oneKg.WithAsymmetricUncertainty(
            upper: 0.1.Percent(),
            lower: 0.1.Fraction());

        var exactlyOneKg = oneKg.WithoutUncertainty();
    }
}
