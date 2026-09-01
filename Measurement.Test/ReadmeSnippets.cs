using Calcusystem.Measurement.Dimensions;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Quantities;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Measurement.Test;

/// <summary>
/// The snippets printed in the READMEs, compiled. Documentation that no longer builds is worse than none, and
/// prose alone cannot show that a shown <c>using</c> list is sufficient.
/// </summary>
public class ReadmeSnippets
{
    [Fact]
    public void RootReadmeQuickStart()
    {
        var mass = Mass.Kilogram.Quantity(2).WithError(1.0.Percent());
        mass.In(Mass.Pound).Should().BeApproximately(4.409, 1e-3);
        mass.RelativeError.Should().BeApproximately(0.01, 1e-12);

        var accel = new Quantity(9.81, Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time))
            .WithError(0.5.Percent());

        var force = mass.Times(accel);
        force.Dimensionality.Should().Be(
            Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time));
    }

    [Fact]
    public void MeasurementReadmeAttachingUncertainty()
    {
        var exact = Mass.Kilogram.Quantity(1).WithoutError();
        var relative = Mass.Kilogram.Quantity(1).WithError(0.1.Percent());
        var absolute = Mass.Kilogram.Quantity(1).WithError(1.0.Units(Mass.Gram));

        var lopsided = Mass.Kilogram.Quantity(1).WithAsymmetricError(
            upper: 0.1.Percent(),
            lower: 2.0.Percent());

        exact.KmsAbsoluteError.Should().Be(0);
        relative.RelativeError.Should().BeApproximately(0.001, 1e-12);
        absolute.KmsAbsoluteError.Should().BeApproximately(0.001, 1e-12);
        lopsided.UpperRelativeError.Should().BeApproximately(0.001, 1e-12);
        lopsided.LowerRelativeError.Should().BeApproximately(0.02, 1e-12);
    }

    [Fact]
    public void PercentAndFractionDifferByAHundred()
    {
        0.1.Percent().Value.Should().BeApproximately(0.001, 1e-12);
        0.1.Fraction().Value.Should().BeApproximately(0.1, 1e-12);
    }
}
