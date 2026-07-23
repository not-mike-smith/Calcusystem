using System.Linq;
using Measurement.BaseClasses;
using Measurement.Extensions;
using Measurement.Uncertainty;
using Measurement.Units;
using Xunit;

namespace Measurement.Test;

public class Sandbox
{
    [Fact]
    public void Play()
    {
        var x = Lists.UnitTypes;

        var oneKgPlusOrMinusAGram = Mass.Kilogram
            .Quantity(1)
            .Measurand(GaussianUncertainty.FromRelErr(0.001));

        var oneKg = Mass.Kilogram.Quantity(1);

        var oneKgPlusOrMinusAMilligram = oneKg
            .Measurand(GaussianUncertainty.FromAbsErr(1.0.Units(Mass.Milligram)));
    }
}
