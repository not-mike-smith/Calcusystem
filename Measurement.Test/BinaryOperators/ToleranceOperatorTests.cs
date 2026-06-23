using DimensionedExpression.BinaryOperators;
using DimensionedExpression.Expressions;
using DimensionedExpression.Interfaces;
using FluentAssertions;
using Measurement;
using Measurement.BaseClasses;
using Measurement.Uncertainty;
using Measurement.Units;
using Xunit;

namespace Measurement.Test.BinaryOperators;

public class ToleranceOperatorTests
{
    // Creates a bound MagnitudeVariable: kmsValue kg, symmetric relativeError
    private static MagnitudeVariable Symmetric(double kmsValue, double relativeError = 0) =>
        new("x", new Magnitude(kmsValue, Mass.Kilogram, relativeError));

    // Creates a bound MagnitudeVariable with independent upper/lower absolute errors (kg)
    private static MagnitudeVariable Asymmetric(double kmsValue, double upperError, double lowerError) =>
        new("x", new Magnitude(new Quantity(kmsValue, Mass.Kilogram), new BoundedUncertainty(upperError, lowerError)));

    private static MagnitudeVariable Unbound() =>
        new("x", Mass.Kilogram.Dimensionality);

    private static WithinBindingToleranceOperator PointOp(MagnitudeVariable lhs, MagnitudeVariable rhs) =>
        new() { Id = "t", Lhs = lhs, Rhs = rhs };
    private static MutuallyWithinToleranceOperator MutualOp(MagnitudeVariable lhs, MagnitudeVariable rhs) =>
        new() { Id = "t", Lhs = lhs, Rhs = rhs };
    private static WhollyWithinToleranceOperator WhollyOp(MagnitudeVariable lhs, MagnitudeVariable rhs) =>
        new() { Id = "t", Lhs = lhs, Rhs = rhs };
    private static AnyToleranceOverlapOperator AnyOp(MagnitudeVariable lhs, MagnitudeVariable rhs) =>
        new() { Id = "t", Lhs = lhs, Rhs = rhs };
    private static PointAndUpperBoundWithinToleranceOperator UpperOp(MagnitudeVariable lhs, MagnitudeVariable rhs) =>
        new() { Id = "t", Lhs = lhs, Rhs = rhs };
    private static PointAndLowerBoundWithinToleranceOperator LowerOp(MagnitudeVariable lhs, MagnitudeVariable rhs) =>
        new() { Id = "t", Lhs = lhs, Rhs = rhs };

    // ── Null / not-fully-described ────────────────────────────────────────────

    [Fact]
    public void AllOperators_ReturnNull_WhenLhsIsUnbound()
    {
        PointOp(Unbound(), Symmetric(10, 0.1)).IsSatisfied().Should().BeNull();
        MutualOp(Unbound(), Symmetric(10, 0.1)).IsSatisfied().Should().BeNull();
        WhollyOp(Unbound(), Symmetric(10, 0.1)).IsSatisfied().Should().BeNull();
        AnyOp(Unbound(), Symmetric(10, 0.1)).IsSatisfied().Should().BeNull();
        UpperOp(Unbound(), Symmetric(10, 0.1)).IsSatisfied().Should().BeNull();
        LowerOp(Unbound(), Symmetric(10, 0.1)).IsSatisfied().Should().BeNull();
    }

    [Fact]
    public void AllOperators_ReturnNull_WhenRhsIsUnbound()
    {
        PointOp(Symmetric(10, 0.1), Unbound()).IsSatisfied().Should().BeNull();
        MutualOp(Symmetric(10, 0.1), Unbound()).IsSatisfied().Should().BeNull();
        WhollyOp(Symmetric(10, 0.1), Unbound()).IsSatisfied().Should().BeNull();
        AnyOp(Symmetric(10, 0.1), Unbound()).IsSatisfied().Should().BeNull();
        UpperOp(Symmetric(10, 0.1), Unbound()).IsSatisfied().Should().BeNull();
        LowerOp(Symmetric(10, 0.1), Unbound()).IsSatisfied().Should().BeNull();
    }

    // ── WithinBindingToleranceOperator (=}) ──────────────────────────────────
    // binding: [kmsValue - lower, kmsValue + upper]; test is the point value only

    [Theory]
    [InlineData(10.0, true)]   // exactly at centre
    [InlineData(10.9, true)]   // inside upper bound
    [InlineData(9.1, true)]    // inside lower bound
    [InlineData(11.1, false)]  // above upper bound
    [InlineData(8.9, false)]   // below lower bound
    public void WithinBindingTolerance_SymmetricBinding(double testKg, bool expected)
    {
        // binding: 10 kg ± 10% → [9, 11]
        var op = PointOp(Symmetric(testKg), Symmetric(10.0, 0.1));
        op.IsSatisfied().Should().Be(expected);
    }

    [Theory]
    [InlineData(11.0, true)]   // inside asymmetric band [9.5, 12]
    [InlineData(9.6, true)]
    [InlineData(12.1, false)]  // above upper (2 kg)
    [InlineData(9.4, false)]   // below lower (0.5 kg)
    public void WithinBindingTolerance_AsymmetricBinding(double testKg, bool expected)
    {
        // binding: 10 kg, upper=2 kg, lower=0.5 kg → [9.5, 12]
        var op = PointOp(Symmetric(testKg), Asymmetric(10.0, 2.0, 0.5));
        op.IsSatisfied().Should().Be(expected);
    }

    // ── MutuallyWithinToleranceOperator (≃) ──────────────────────────────────
    // symmetric: each side's point must lie within the other's band

    [Fact]
    public void MutuallyWithinTolerance_OverlappingBands_IsTrue()
    {
        // lhs: 10 ± 1  → [9, 11];  rhs: 10.5 ± 1 → [9.5, 11.5]
        // 10 ∈ [9.5, 11.5] ✓ and 10.5 ∈ [9, 11] ✓
        MutualOp(Symmetric(10.0, 0.1), Symmetric(10.5, 0.1))
            .IsSatisfied().Should().BeTrue();
    }

    [Fact]
    public void MutuallyWithinTolerance_NonOverlapping_IsFalse()
    {
        // lhs: 10 ± 0.2  → [9.8, 10.2];  rhs: 11 ± 0.2 → [10.8, 11.2]
        // 10 ∉ [10.8, 11.2] → false
        MutualOp(Symmetric(10.0, 0.02), Symmetric(11.0, 0.02))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void MutuallyWithinTolerance_IsCommutative()
    {
        var lhs = Symmetric(10.0, 0.1);
        var rhs = Symmetric(10.5, 0.1);
        MutualOp(lhs, rhs).IsSatisfied()
            .Should().Be(MutualOp(rhs, lhs).IsSatisfied());
    }

    // ── WhollyWithinToleranceOperator ([=}) ──────────────────────────────────
    // test interval [test - lower, test + upper] must be strictly inside binding interval

    [Fact]
    public void WhollyWithinTolerance_NarrowInsideWide_IsTrue()
    {
        // test: 10 ± 0.5 kg → [9.5, 10.5];  binding: 10 ± 1 kg → [9, 11]
        WhollyOp(Symmetric(10.0, 0.05), Symmetric(10.0, 0.1))
            .IsSatisfied().Should().BeTrue();
    }

    [Fact]
    public void WhollyWithinTolerance_WideInsideNarrow_IsFalse()
    {
        // test: 10 ± 1.5 kg → [8.5, 11.5];  binding: 10 ± 1 kg → [9, 11]
        WhollyOp(Symmetric(10.0, 0.15), Symmetric(10.0, 0.1))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void WhollyWithinTolerance_EqualIntervals_IsFalse()
    {
        // strictly inside — equal boundaries do not satisfy the strict inequality
        WhollyOp(Symmetric(10.0, 0.1), Symmetric(10.0, 0.1))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void WhollyWithinTolerance_AsymmetricBounds()
    {
        // test: 10 kg, upper=1 lower=0.4 → [9.6, 11];  binding: upper=1.5 lower=0.5 → [9.5, 11.5]
        // [9.6, 11] strictly inside [9.5, 11.5] → true
        WhollyOp(Asymmetric(10.0, 1.0, 0.4), Asymmetric(10.0, 1.5, 0.5))
            .IsSatisfied().Should().BeTrue();
    }

    // ── AnyToleranceOverlapOperator (≈) ──────────────────────────────────────
    // intervals overlap if smaller.upper >= bigger.lower

    [Fact]
    public void AnyToleranceOverlap_BarellyTouching_IsTrue()
    {
        // lhs: 10 ± 0.5 → [9.5, 10.5];  rhs: 11 ± 0.5 → [10.5, 11.5]
        // smaller upper (10.5) >= bigger lower (10.5) → touch → true
        AnyOp(Symmetric(10.0, 0.05), Symmetric(11.0, 0.05))
            .IsSatisfied().Should().BeTrue();
    }

    [Fact]
    public void AnyToleranceOverlap_Gap_IsFalse()
    {
        // lhs: 10 ± 0.4 → [9.6, 10.4];  rhs: 11 ± 0.4 → [10.6, 11.4]
        // smaller upper (10.4) < bigger lower (10.6) → gap → false
        AnyOp(Symmetric(10.0, 0.04), Symmetric(11.0, 0.04))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void AnyToleranceOverlap_IsCommutative()
    {
        var lhs = Symmetric(10.0, 0.05);
        var rhs = Symmetric(11.0, 0.05);
        AnyOp(lhs, rhs).IsSatisfied()
            .Should().Be(AnyOp(rhs, lhs).IsSatisfied());
    }

    [Fact]
    public void AnyToleranceOverlap_AsymmetricBounds_UsesDirectionalErrors()
    {
        // smaller: 10 kg, upper=0.3 → upper bound = 10.3
        // bigger: 11 kg, lower=0.5 → lower bound = 10.5
        // 10.3 < 10.5 → no overlap → false
        AnyOp(Asymmetric(10.0, 0.3, 1.0), Asymmetric(11.0, 1.0, 0.5))
            .IsSatisfied().Should().BeFalse();

        // smaller: 10 kg, upper=0.6 → upper bound = 10.6
        // bigger: 11 kg, lower=0.5 → lower bound = 10.5
        // 10.6 >= 10.5 → overlap → true
        AnyOp(Asymmetric(10.0, 0.6, 1.0), Asymmetric(11.0, 1.0, 0.5))
            .IsSatisfied().Should().BeTrue();
    }

    // ── PointAndUpperBoundWithinToleranceOperator ([≓}) ──────────────────────
    // test point >= binding lower AND test upper <= binding upper

    [Fact]
    public void PointAndUpperBound_BothConditionsMet_IsTrue()
    {
        // test: 10 ± 1 → upper=11;  binding: 10 ± 2 → [8, 12]
        // 10 >= 8 ✓ and 11 <= 12 ✓
        UpperOp(Symmetric(10.0, 0.1), Symmetric(10.0, 0.2))
            .IsSatisfied().Should().BeTrue();
    }

    [Fact]
    public void PointAndUpperBound_TestUpperExceedsBinding_IsFalse()
    {
        // test: 11 ± 2 → upper=13;  binding: 10 ± 2 → [8, 12]
        // 13 > 12 → false
        UpperOp(Symmetric(11.0, 0.2), Symmetric(10.0, 0.2))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void PointAndUpperBound_TestPointBelowBindingLower_IsFalse()
    {
        // test: 7 ± 0.1;  binding: 10 ± 2 → lower=8
        // 7 < 8 → false
        UpperOp(Symmetric(7.0, 0.01), Symmetric(10.0, 0.2))
            .IsSatisfied().Should().BeFalse();
    }

    // ── PointAndLowerBoundWithinToleranceOperator ([≒}) ──────────────────────
    // test point <= binding upper AND test lower >= binding lower

    [Fact]
    public void PointAndLowerBound_BothConditionsMet_IsTrue()
    {
        // test: 10 ± 1 → lower=9;  binding: 10 ± 2 → [8, 12]
        // 10 <= 12 ✓ and 9 >= 8 ✓
        LowerOp(Symmetric(10.0, 0.1), Symmetric(10.0, 0.2))
            .IsSatisfied().Should().BeTrue();
    }

    [Fact]
    public void PointAndLowerBound_TestLowerBelowBindingLower_IsFalse()
    {
        // test: 9 ± 2 → lower=7;  binding: 10 ± 2 → lower=8
        // 7 < 8 → false
        LowerOp(Symmetric(9.0, 0.2), Symmetric(10.0, 0.2))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void PointAndLowerBound_TestPointAboveBindingUpper_IsFalse()
    {
        // test: 13 ± 0.1;  binding: 10 ± 2 → upper=12
        // 13 > 12 → false
        LowerOp(Symmetric(13.0, 0.01), Symmetric(10.0, 0.2))
            .IsSatisfied().Should().BeFalse();
    }

    // ── EqualityOperator (==) ────────────────────────────────────────────────

    [Fact]
    public void EqualityOperator_DelegatesToInjectedEstimator_WhenTrue()
    {
        var op = new EqualityOperator(new AlwaysTrueEstimator())
        {
            Id = "test",
            Lhs = Symmetric(10.0),
            Rhs = Symmetric(10.0)
        };
        op.IsSatisfied().Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_DelegatesToInjectedEstimator_WhenFalse()
    {
        var op = new EqualityOperator(new AlwaysFalseEstimator())
        {
            Id = "test",
            Lhs = Symmetric(10.0),
            Rhs = Symmetric(10.0)
        };
        op.IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_ReturnsNull_WhenNotFullyDescribed()
    {
        var op = new EqualityOperator(new AlwaysTrueEstimator())
        {
            Id = "test",
            Lhs = Unbound(),
            Rhs = Symmetric(10.0)
        };
        op.IsSatisfied().Should().BeNull();
    }

    private class AlwaysTrueEstimator : IEqualityEstimating
    {
        public bool AreEqual(PrecisionQuantity lhs, PrecisionQuantity rhs) => true;
    }

    private class AlwaysFalseEstimator : IEqualityEstimating
    {
        public bool AreEqual(PrecisionQuantity lhs, PrecisionQuantity rhs) => false;
    }
}
