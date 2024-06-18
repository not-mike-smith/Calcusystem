using Measurement.Extensions;
using FluentAssertions;
using Xunit;

namespace Measurement.Test.Extensions;

public class IntExponentExtensionsTests
{
    [Theory]
    [InlineData(0, "⁰")]
    [InlineData(-1, "⁻¹")]
    [InlineData(1, "¹")]
    [InlineData(3123, "³¹²³")]
    public void ToSuperscript(int i, string expected)
    {
        i.ToSuperscript().Should().Be(expected);
    }
}
