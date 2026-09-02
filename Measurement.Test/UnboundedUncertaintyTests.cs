using Calcusystem.Measurement.Comparison;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Measurement.Test;

/// <summary>
/// What an unbounded error bar does to a comparison. Uncertainty supplies the <i>scale</i> at which two values
/// count as the same, so an uncertainty with no bound has to be kept from supplying one.
/// </summary>
public class UnboundedUncertaintyTests
{
    private static Measurand Kg(double value, double absoluteError) =>
        Mass.Kilogram.Quantity(value).Measurand(
            SymmetricUncertainty.FromAbsErr(Mass.Kilogram.Quantity(absoluteError)));

    /// <remarks>
    /// A regression, and it was not subtle: the near-zero threshold is a fraction of the finest error bar, so an
    /// infinite bar made the threshold infinite and <i>every</i> finite value fell below it. Five kilograms
    /// compared equal to ten. An unbounded uncertainty means the measurement resolves nothing, which is a
    /// different claim from the two values agreeing.
    /// </remarks>
    [Fact]
    public void AnUnboundedErrorBarDoesNotMakeEveryValueAgreeWithEveryOther()
    {
        var five = Kg(5, double.PositiveInfinity);
        var ten = Kg(10, double.PositiveInfinity);

        MeasurandComparer.Compare(five, Landmark.Nominal, ten, Landmark.Nominal)
            .Should().Be(ComparisonResult.LessThan);
    }

    /// <remarks>
    /// The bounds themselves genuinely have no answer, which is the case the three-valued seam exists for: two
    /// quantities that both grew without bound say nothing about which outgrew the other.
    /// </remarks>
    [Fact]
    public void TwoUnboundedCeilingsAreIncomparableRatherThanEqual() =>
        MeasurandComparer.Compare(
            Kg(5, double.PositiveInfinity), Landmark.UpperBound,
            Kg(10, double.PositiveInfinity), Landmark.UpperBound)
        .Should().Be(ComparisonResult.Incomparable);

    /// <remarks>
    /// A finite bar alongside an unbounded one still sets the scale. The unbounded side has no opinion about
    /// resolution, exactly as an exact value has none — both are skipped when the threshold is chosen.
    /// </remarks>
    [Fact]
    public void AFiniteErrorBarStillSetsTheScaleWhenTheOtherIsUnbounded()
    {
        var tiny = Kg(1e-20, 1e-9);
        var alsoTiny = Kg(-1e-20, double.PositiveInfinity);

        MeasurandComparer.Compare(tiny, Landmark.Nominal, alsoTiny, Landmark.Nominal)
            .Should().Be(ComparisonResult.Equal, "both sit far below what 1e-9 can resolve");
    }

    /// <remarks>
    /// The behaviour change this whole slice introduces, stated where it can be seen. Comparison is now
    /// tolerance-aware, so an ordering that exists only in the last bits of a mantissa is no ordering at all —
    /// which is what stops a chain of arithmetic from manufacturing a confident verdict out of rounding.
    /// </remarks>
    [Fact]
    public void ValuesSeparatedOnlyByFloatingPointDriftAreNotOrdered()
    {
        var a = Kg(1.0, 0);
        var b = Kg(1.0 + 1e-15, 0);

        (a.KmsValue < b.KmsValue).Should().BeTrue("raw < still orders them");
        MeasurandComparer.Compare(a, Landmark.Nominal, b, Landmark.Nominal)
            .Should().Be(ComparisonResult.Equal, "but they are the same number to any measurement");
    }
}
