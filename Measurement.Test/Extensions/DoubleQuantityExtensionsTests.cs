using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Measurement.Test.Extensions;

public class DoubleQuantityExtensionsTests
{
    private static readonly UnitOfMeasure Seconds = new UnitOfMeasure(Dimensionality.Time, "s", 1);

    [Fact]
    public void HappyPath()
    {
        var min = 60d.Units(Seconds);
        min.In(Seconds).Should().Be(60);
    }
}
