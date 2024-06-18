using Measurement.Extensions;
using FluentAssertions;
using Measurement.Models;
using Xunit;

namespace Measurement.Test.Extensions;

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
