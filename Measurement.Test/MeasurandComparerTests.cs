using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.Measurement.Test;

/// <summary>
/// The single place a numeric comparison happens. Everything above it — operators, confidence ladders — chooses
/// which landmarks to compare and what to do with the answer; none of them decides what "less than" means, so
/// what is pinned here is what the whole comparison layer means.
/// </summary>
public class MeasurandComparerTests
{
    private static Measurand Meters(double value, double absoluteError = 0) =>
        Length.Meter.Quantity(value).Measurand(
            AsymmetricUncertainty.FromAbsErr(
                Length.Meter.Quantity(absoluteError), Length.Meter.Quantity(absoluteError)));

    private static Measurand Kilograms(double value) =>
        Mass.Kilogram.Quantity(value).WithoutError();

    private static Measurand ExactMeters(double value) =>
        Length.Meter.Quantity(value).WithoutError();

    private static ComparisonResult Compare(
        Measurand lhs, Measurand rhs,
        Landmark lhsLandmark = Landmark.Nominal, Landmark rhsLandmark = Landmark.Nominal) =>
        MeasurandComparer.Compare(lhs, lhsLandmark, rhs, rhsLandmark);

    // ── Incomparable: questions with no answer ────────────────────────────────

    /// <remarks>
    /// Not <see cref="ComparisonResult.Equal"/> and not an ordering. Kilograms and metres are not <i>unequal</i>
    /// — answering that would let "not equal" read as true and put a confident ordering on quantities sharing no
    /// scale. This is also the check no binary operator used to perform at all.
    /// </remarks>
    [Fact]
    public void DifferentDimensionsAreIncomparable()
    {
        Compare(Meters(1), Kilograms(1)).Should().Be(ComparisonResult.Incomparable);
        Compare(Kilograms(1), Meters(1)).Should().Be(ComparisonResult.Incomparable);

        // Even where the raw KMS numbers would compare perfectly well.
        Compare(Meters(1), Kilograms(2)).Should().Be(ComparisonResult.Incomparable);
    }

    [Fact]
    public void NaNIsIncomparableWithAnything()
    {
        var nan = ExactMeters(double.NaN);

        Compare(nan, Meters(1)).Should().Be(ComparisonResult.Incomparable);
        Compare(Meters(1), nan).Should().Be(ComparisonResult.Incomparable);
        Compare(nan, nan).Should().Be(ComparisonResult.Incomparable);
    }

    /// <remarks>
    /// IEEE calls two like infinities equal. They are not: each stands for "grew without bound", which says
    /// nothing about whether one outgrew the other, so reporting equality manufactures agreement out of two
    /// unknowns.
    /// </remarks>
    [Fact]
    public void TwoInfinitiesOfTheSameSignAreIncomparable()
    {
        var positive = ExactMeters(double.PositiveInfinity);
        var negative = ExactMeters(double.NegativeInfinity);

        Compare(positive, positive).Should().Be(ComparisonResult.Incomparable);
        Compare(negative, negative).Should().Be(ComparisonResult.Incomparable);
    }

    /// <remarks>Opposite signs, and infinity against a bounded value, are perfectly ordered.</remarks>
    [Fact]
    public void InfinitiesThatAreOrderedAreReportedAsOrdered()
    {
        var positive = ExactMeters(double.PositiveInfinity);
        var negative = ExactMeters(double.NegativeInfinity);

        Compare(negative, positive).Should().Be(ComparisonResult.LessThan);
        Compare(positive, negative).Should().Be(ComparisonResult.GreaterThan);

        Compare(negative, Meters(0)).Should().Be(ComparisonResult.LessThan);
        Compare(Meters(0), positive).Should().Be(ComparisonResult.LessThan);
        Compare(positive, Meters(0)).Should().Be(ComparisonResult.GreaterThan);
        Compare(Meters(0), negative).Should().Be(ComparisonResult.GreaterThan);
    }

    // ── Ordinary comparison ───────────────────────────────────────────────────

    [Fact]
    public void PlainlyDifferentValuesAreOrdered()
    {
        Compare(Meters(1, 0.1), Meters(2, 0.1)).Should().Be(ComparisonResult.LessThan);
        Compare(Meters(2, 0.1), Meters(1, 0.1)).Should().Be(ComparisonResult.GreaterThan);
        Compare(Meters(1, 0.1), Meters(1, 0.1)).Should().Be(ComparisonResult.Equal);
    }

    /// <remarks>
    /// Values that differ only by drift down a chain of arithmetic are the same number. Sized for a double's
    /// ~15-16 significant digits with a few lost along the way — not for measurement uncertainty, which is a
    /// separate and much coarser question.
    /// </remarks>
    [Fact]
    public void ValuesWithinRelativeDriftAreEqual()
    {
        Compare(ExactMeters(10), ExactMeters(10 + 1e-13)).Should().Be(ComparisonResult.Equal);
        Compare(ExactMeters(10), ExactMeters(10 + 1e-8)).Should().Be(ComparisonResult.LessThan);
    }

    // ── Near zero: the uncertainty supplies the scale ─────────────────────────

    /// <remarks>
    /// <para>
    /// The case the relative test cannot answer. Values straddling zero are always far apart <i>relatively</i>,
    /// however tiny, because the denominator shrinks with them — so whether they are "the same" needs a scale,
    /// and only the measurement carries one.
    /// </para>
    /// <para>
    /// The two rows below are the same two numbers with different error bars and opposite answers. No universal
    /// constant, and no per-dimension constant, could distinguish them.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(1e-9, ComparisonResult.Equal)]        // coarse: a thousandth of a sigma apart
    [InlineData(1e-18, ComparisonResult.GreaterThan)] // fine:   the bar resolves them
    public void WhetherValuesStraddlingZeroAgreeDependsOnTheirUncertainty(
        double absoluteError, ComparisonResult expected)
    {
        Compare(Meters(1e-12, absoluteError), Meters(-1e-12, absoluteError)).Should().Be(expected);
    }

    [Fact]
    public void ValuesFarBelowTheResolutionOfTheMeasurementAreZero()
    {
        Compare(Meters(1e-30, 1e-9), Meters(-1e-30, 1e-9)).Should().Be(ComparisonResult.Equal);
    }

    /// <remarks>
    /// Near-zero equality needs <i>both</i> operands below the threshold. A tiny value against a large one is
    /// still ordered, however far below the resolution the tiny one sits.
    /// </remarks>
    [Fact]
    public void OnlyOneValueBeingNearZeroIsNotEnough()
    {
        Compare(Meters(1e-30, 1e-9), Meters(10, 1e-9)).Should().Be(ComparisonResult.LessThan);
    }

    /// <remarks>
    /// An exact operand has no error bar, and that must not be read as an error bar of zero. It does not make
    /// the other operand better resolved — it simply has no opinion — and letting the zero win the minimum
    /// would collapse the threshold to the dimensional floor. Comparing a measurement against an exact limit of
    /// zero is ordinary, so this is not a corner case.
    /// </remarks>
    [Fact]
    public void AnExactOperandDoesNotEraseTheOthersUncertainty()
    {
        Compare(ExactMeters(0), Meters(1e-20, 1e-9)).Should().Be(ComparisonResult.Equal);
        Compare(Meters(1e-20, 1e-9), ExactMeters(0)).Should().Be(ComparisonResult.Equal);
    }

    /// <remarks>
    /// With no uncertainty anywhere there is no measurement scale to appeal to, so only the dimension's own
    /// floor applies — and that is the Planck length, some twenty-five orders below any engineering tolerance.
    /// It catches the physically absurd, never the practically negligible.
    /// </remarks>
    [Fact]
    public void WithNoUncertaintyAnywhereOnlyTheDimensionalFloorApplies()
    {
        Compare(ExactMeters(0), ExactMeters(1e-20)).Should().Be(ComparisonResult.LessThan);

        // Below the Planck length, the dimensional floor does fire.
        Compare(ExactMeters(0), ExactMeters(1e-40)).Should().Be(ComparisonResult.Equal);
    }

    // ── Landmarks ─────────────────────────────────────────────────────────────

    /// <remarks>
    /// The comparer's whole job above the arithmetic: it compares the two landmarks it is handed, and the
    /// choice of landmark is what distinguishes one operator from another.
    /// </remarks>
    [Fact]
    public void TheLandmarksChosenDecideWhatIsCompared()
    {
        var subject = Meters(10, 1);  // [9, 11]
        var band = Meters(11, 1);     // [10, 12]

        Compare(subject, band, Landmark.Nominal, Landmark.Nominal).Should().Be(ComparisonResult.LessThan);
        Compare(subject, band, Landmark.UpperBound, Landmark.LowerBound).Should().Be(ComparisonResult.GreaterThan);
        Compare(subject, band, Landmark.UpperBound, Landmark.Nominal).Should().Be(ComparisonResult.Equal);
    }

    [Fact]
    public void SwappingTheOperandsMirrorsTheResult()
    {
        var a = Meters(10, 1);
        var b = Meters(11, 1);

        foreach (var lhs in new[] { Landmark.LowerBound, Landmark.Nominal, Landmark.UpperBound })
        {
            foreach (var rhs in new[] { Landmark.LowerBound, Landmark.Nominal, Landmark.UpperBound })
            {
                var forward = MeasurandComparer.Compare(a, lhs, b, rhs);
                var backward = MeasurandComparer.Compare(b, rhs, a, lhs);

                backward.Should().Be(Mirror(forward), $"{lhs} vs {rhs}");
            }
        }
    }

    private static ComparisonResult Mirror(ComparisonResult result) => result switch
    {
        ComparisonResult.LessThan => ComparisonResult.GreaterThan,
        ComparisonResult.GreaterThan => ComparisonResult.LessThan,
        _ => result,
    };

    // ── Ladder soundness ──────────────────────────────────────────────────────

    /// <summary>
    /// The property the confidence ladders rest on: comparison is <b>monotone</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ladders' implication chains — <c>Certain ⟹ Nominal ⟹ Possible</c>, and containment's downward
    /// implications — hold only if the comparison matrix stays a monotone staircase, which needs this: sweeping
    /// the left value upward, the answer may only ever move <i>forward</i> through LessThan → Equal →
    /// GreaterThan. It must never go back.
    /// </para>
    /// <para>
    /// Equivalently, the values judged equal to a fixed <c>y</c> form one contiguous interval containing
    /// <c>y</c>. A tolerance-aware comparison is not guaranteed this — an arbitrary strategy could call distant
    /// values equal and near ones not — which is exactly why the comparer is concrete rather than injected.
    /// Making it concrete is what turns this from a hope into an assertion.
    /// </para>
    /// <para>
    /// Negative and zero <c>y</c> values are in the sweep deliberately: relative scaling is least obvious there,
    /// and the near-zero branch only engages around zero.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(-1e5, 1e-9)]
    [InlineData(-10, 1e-9)]
    [InlineData(-1, 0)]
    [InlineData(-1e-13, 1e-9)]
    [InlineData(0, 1e-9)]
    [InlineData(0, 0)]
    [InlineData(1e-13, 1e-9)]
    [InlineData(1, 0)]
    [InlineData(10, 1e-3)]
    [InlineData(1e5, 1e-9)]
    public void ComparisonIsMonotoneInTheLeftValue(double rhsValue, double absoluteError)
    {
        double[] ascending =
        [
            -1e5, -10, -1, -1e-6, -1e-12, -1e-13, -1e-20, -1e-40,
            0,
            1e-40, 1e-20, 1e-13, 1e-12, 1e-6, 1, 10, 1e5,
        ];

        var rhs = Meters(rhsValue, absoluteError);

        var previous = -1;
        var previousValue = double.NegativeInfinity;

        foreach (var lhsValue in ascending)
        {
            var result = Compare(Meters(lhsValue, absoluteError), rhs);
            result.Should().NotBe(ComparisonResult.Incomparable, "every value here is finite and a length");

            var rank = Rank(result);
            rank.Should().BeGreaterThanOrEqualTo(
                previous,
                $"comparing {lhsValue} against {rhsValue} (±{absoluteError}) gave {result}, but " +
                $"{previousValue} — which is smaller — had already reached a later tier");

            previous = rank;
            previousValue = lhsValue;
        }
    }

    private static int Rank(ComparisonResult result) => result switch
    {
        ComparisonResult.LessThan => 0,
        ComparisonResult.Equal => 1,
        ComparisonResult.GreaterThan => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
    };

    /// <remarks>
    /// The same property stated the other way round, and the one the ladder argument actually invokes: if a
    /// value is judged strictly below a bound, everything below it is too. This is what lets
    /// <c>aU &lt; bL</c> imply <c>a &lt; bL</c> without re-deriving anything.
    /// </remarks>
    [Fact]
    public void AnythingBelowAValueJudgedLessThanIsAlsoLessThan()
    {
        var bound = Meters(1, 1e-9);

        double[] descending = [0.9, 0.5, 0, -0.5, -1, -1e5];
        var previous = Meters(0.9, 1e-9);

        Compare(previous, bound).Should().Be(ComparisonResult.LessThan);

        foreach (var value in descending)
        {
            Compare(Meters(value, 1e-9), bound)
                .Should().Be(ComparisonResult.LessThan, $"{value} is at or below 0.9, which is already less");
        }
    }
}
