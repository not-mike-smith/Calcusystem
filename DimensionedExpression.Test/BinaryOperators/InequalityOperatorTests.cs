using DimensionedExpression.BinaryOperators;
using DimensionedExpression.Expressions;
using FluentAssertions;
using Measurement;
using Measurement.Uncertainty;
using Measurement.Units;
using Xunit;

namespace DimensionedExpression.Test.BinaryOperators;

public class InequalityOperatorTests
{
    private static MagnitudeVariable Symmetric(double kmsValue, double relativeError = 0) =>
        new("x", new Magnitude(kmsValue, Mass.Kilogram, relativeError));

    private static MagnitudeVariable Asymmetric(double kmsValue, double upperError, double lowerError) =>
        new("x", new Magnitude(new Quantity(kmsValue, Mass.Kilogram), new BoundedUncertainty(upperError, lowerError)));

    private static MagnitudeVariable Unbound() =>
        new("x", Mass.Kilogram.Dimensionality);

    private static DefinitelyLessThanOperator DefLess(MagnitudeVariable lhs, MagnitudeVariable rhs) =>
        new() { Id = "t", Lhs = lhs, Rhs = rhs };
    private static UpperBoundsLessThanOperator UpperLess(MagnitudeVariable lhs, MagnitudeVariable rhs) =>
        new() { Id = "t", Lhs = lhs, Rhs = rhs };
    private static NominallyLessThanOperator NomLess(MagnitudeVariable lhs, MagnitudeVariable rhs) =>
        new() { Id = "t", Lhs = lhs, Rhs = rhs };
    private static DefinitelyGreaterThanOperator DefGreater(MagnitudeVariable lhs, MagnitudeVariable rhs) =>
        new() { Id = "t", Lhs = lhs, Rhs = rhs };
    private static LowerBoundsGreaterThanOperator LowerGreater(MagnitudeVariable lhs, MagnitudeVariable rhs) =>
        new() { Id = "t", Lhs = lhs, Rhs = rhs };
    private static NominallyGreaterThanOperator NomGreater(MagnitudeVariable lhs, MagnitudeVariable rhs) =>
        new() { Id = "t", Lhs = lhs, Rhs = rhs };

    // ── Null / not-fully-described ────────────────────────────────────────────

    [Fact]
    public void AllOperators_ReturnNull_WhenLhsIsUnbound()
    {
        var rhs = Symmetric(10, 0.1);
        DefLess(Unbound(), rhs).IsSatisfied().Should().BeNull();
        UpperLess(Unbound(), rhs).IsSatisfied().Should().BeNull();
        NomLess(Unbound(), rhs).IsSatisfied().Should().BeNull();
        DefGreater(Unbound(), rhs).IsSatisfied().Should().BeNull();
        LowerGreater(Unbound(), rhs).IsSatisfied().Should().BeNull();
        NomGreater(Unbound(), rhs).IsSatisfied().Should().BeNull();
    }

    [Fact]
    public void AllOperators_ReturnNull_WhenRhsIsUnbound()
    {
        var lhs = Symmetric(10, 0.1);
        DefLess(lhs, Unbound()).IsSatisfied().Should().BeNull();
        UpperLess(lhs, Unbound()).IsSatisfied().Should().BeNull();
        NomLess(lhs, Unbound()).IsSatisfied().Should().BeNull();
        DefGreater(lhs, Unbound()).IsSatisfied().Should().BeNull();
        LowerGreater(lhs, Unbound()).IsSatisfied().Should().BeNull();
        NomGreater(lhs, Unbound()).IsSatisfied().Should().BeNull();
    }

    // ── DefinitelyLessThanOperator (<<) ──────────────────────────────────────
    // Lhs.Upper < Rhs.Lower: entire intervals non-overlapping

    [Fact]
    public void DefinitelyLessThan_ClearGap_IsTrue()
    {
        // Lhs: 5 ± 10% → upper = 5.5;  Rhs: 10 ± 10% → lower = 9
        // 5.5 < 9 → true
        DefLess(Symmetric(5, 0.1), Symmetric(10, 0.1))
            .IsSatisfied().Should().BeTrue();
    }

    [Fact]
    public void DefinitelyLessThan_IntervalsOverlap_IsFalse()
    {
        // Lhs: 9 ± 10% → upper = 9.9;  Rhs: 10 ± 10% → lower = 9
        // 9.9 < 9 → false
        DefLess(Symmetric(9, 0.1), Symmetric(10, 0.1))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void DefinitelyLessThan_LhsGreaterThanRhs_IsFalse()
    {
        DefLess(Symmetric(10, 0), Symmetric(5, 0))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void DefinitelyLessThan_AsymmetricBounds_UsesDirectionalErrors()
    {
        // Lhs: 5 kg, upperError=1 → upper=6;  Rhs: 10 kg, lowerError=2 → lower=8
        // 6 < 8 → true
        DefLess(Asymmetric(5, 1.0, 0.5), Asymmetric(10, 0.5, 2.0))
            .IsSatisfied().Should().BeTrue();

        // Lhs: 5 kg, upperError=3 → upper=8;  same Rhs lower=8
        // 8 < 8 → false (strict)
        DefLess(Asymmetric(5, 3.0, 0.5), Asymmetric(10, 0.5, 2.0))
            .IsSatisfied().Should().BeFalse();
    }

    // ── UpperBoundsLessThanOperator (<^) ─────────────────────────────────────
    // Lhs.Upper < Rhs.Upper: ceilings compared; intervals may overlap

    [Fact]
    public void UpperBoundsLessThan_OverlappingIntervals_CanBeTrue()
    {
        // Lhs: 9 ± 10% → upper=9.9;  Rhs: 10 ± 10% → upper=11
        // intervals overlap ([8.1,9.9] vs [9,11]), but 9.9 < 11 → true
        UpperLess(Symmetric(9, 0.1), Symmetric(10, 0.1))
            .IsSatisfied().Should().BeTrue();
    }

    [Fact]
    public void UpperBoundsLessThan_LhsUpperExceedsRhsUpper_IsFalse()
    {
        // Lhs: 11 ± 10% → upper=12.1;  Rhs: 10 ± 10% → upper=11
        // 12.1 < 11 → false
        UpperLess(Symmetric(11, 0.1), Symmetric(10, 0.1))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void UpperBoundsLessThan_IsWeakerThanDefinitely()
    {
        // This case passes UpperBounds but fails Definitely:
        // Lhs: 9 ± 10% → upper=9.9, Rhs: 10 ± 10% → lower=9, upper=11
        // DefinitelyLessThan: 9.9 < 9 → false
        // UpperBoundsLessThan: 9.9 < 11 → true
        var lhs = Symmetric(9, 0.1);
        var rhs = Symmetric(10, 0.1);
        DefLess(lhs, rhs).IsSatisfied().Should().BeFalse();
        UpperLess(lhs, rhs).IsSatisfied().Should().BeTrue();
    }

    // ── NominallyLessThanOperator (<~) ───────────────────────────────────────
    // Lhs.KmsValue < Rhs.KmsValue: nominal values only, uncertainty ignored

    [Fact]
    public void NominallyLessThan_LargeOverlappingErrors_IgnoresUncertainty()
    {
        // Lhs: 9 ± 50% → [4.5, 13.5];  Rhs: 10 ± 50% → [5, 15]
        // Intervals heavily overlap, but 9 < 10 → true
        NomLess(Symmetric(9, 0.5), Symmetric(10, 0.5))
            .IsSatisfied().Should().BeTrue();
    }

    [Fact]
    public void NominallyLessThan_NominalReversed_IsFalse()
    {
        NomLess(Symmetric(10, 0), Symmetric(9, 0))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void NominallyLessThan_EqualNominals_IsFalse()
    {
        NomLess(Symmetric(10, 0.1), Symmetric(10, 0.1))
            .IsSatisfied().Should().BeFalse();
    }

    // ── DefinitelyGreaterThanOperator (>>) ───────────────────────────────────
    // Lhs.Lower > Rhs.Upper: entire intervals non-overlapping

    [Fact]
    public void DefinitelyGreaterThan_ClearGap_IsTrue()
    {
        // Lhs: 10 ± 10% → lower=9;  Rhs: 5 ± 10% → upper=5.5
        // 9 > 5.5 → true
        DefGreater(Symmetric(10, 0.1), Symmetric(5, 0.1))
            .IsSatisfied().Should().BeTrue();
    }

    [Fact]
    public void DefinitelyGreaterThan_IntervalsOverlap_IsFalse()
    {
        // Lhs: 10 ± 10% → lower=9;  Rhs: 9 ± 10% → upper=9.9
        // 9 > 9.9 → false
        DefGreater(Symmetric(10, 0.1), Symmetric(9, 0.1))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void DefinitelyGreaterThan_AsymmetricBounds_UsesDirectionalErrors()
    {
        // Lhs: 10 kg, lowerError=1 → lower=9;  Rhs: 5 kg, upperError=3 → upper=8
        // 9 > 8 → true
        DefGreater(Asymmetric(10, 0.5, 1.0), Asymmetric(5, 3.0, 0.5))
            .IsSatisfied().Should().BeTrue();

        // Rhs: 5 kg, upperError=4 → upper=9
        // 9 > 9 → false (strict)
        DefGreater(Asymmetric(10, 0.5, 1.0), Asymmetric(5, 4.0, 0.5))
            .IsSatisfied().Should().BeFalse();
    }

    // ── LowerBoundsGreaterThanOperator (>v) ──────────────────────────────────
    // Lhs.Lower > Rhs.Lower: floors compared; intervals may overlap

    [Fact]
    public void LowerBoundsGreaterThan_OverlappingIntervals_CanBeTrue()
    {
        // Lhs: 10 ± 10% → lower=9;  Rhs: 9 ± 10% → lower=8.1
        // intervals overlap ([9,11] vs [8.1,9.9]), but 9 > 8.1 → true
        LowerGreater(Symmetric(10, 0.1), Symmetric(9, 0.1))
            .IsSatisfied().Should().BeTrue();
    }

    [Fact]
    public void LowerBoundsGreaterThan_LhsLowerBelowRhsLower_IsFalse()
    {
        // Lhs: 9 ± 10% → lower=8.1;  Rhs: 10 ± 10% → lower=9
        // 8.1 > 9 → false
        LowerGreater(Symmetric(9, 0.1), Symmetric(10, 0.1))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void LowerBoundsGreaterThan_IsWeakerThanDefinitely()
    {
        // Lhs: 10 ± 10%, Rhs: 9 ± 10%
        // DefinitelyGreaterThan: Lhs.lower=9 > Rhs.upper=9.9 → false
        // LowerBoundsGreaterThan: Lhs.lower=9 > Rhs.lower=8.1 → true
        var lhs = Symmetric(10, 0.1);
        var rhs = Symmetric(9, 0.1);
        DefGreater(lhs, rhs).IsSatisfied().Should().BeFalse();
        LowerGreater(lhs, rhs).IsSatisfied().Should().BeTrue();
    }

    // ── NominallyGreaterThanOperator (>~) ────────────────────────────────────
    // Lhs.KmsValue > Rhs.KmsValue: nominal values only, uncertainty ignored

    [Fact]
    public void NominallyGreaterThan_LargeOverlappingErrors_IgnoresUncertainty()
    {
        NomGreater(Symmetric(10, 0.5), Symmetric(9, 0.5))
            .IsSatisfied().Should().BeTrue();
    }

    [Fact]
    public void NominallyGreaterThan_NominalReversed_IsFalse()
    {
        NomGreater(Symmetric(9, 0), Symmetric(10, 0))
            .IsSatisfied().Should().BeFalse();
    }

    [Fact]
    public void NominallyGreaterThan_EqualNominals_IsFalse()
    {
        NomGreater(Symmetric(10, 0.1), Symmetric(10, 0.1))
            .IsSatisfied().Should().BeFalse();
    }

    // ── Less/Greater symmetry ─────────────────────────────────────────────────
    // LessThan(a, b) should equal GreaterThan(b, a) for each strictness level

    [Theory]
    [InlineData(5.0, 10.0)]
    [InlineData(10.0, 5.0)]
    [InlineData(10.0, 10.0)]
    public void DefinitelyLessThan_MatchesDefinitelyGreaterThanWithSwappedArgs(double a, double b)
    {
        DefLess(Symmetric(a, 0.1), Symmetric(b, 0.1)).IsSatisfied()
            .Should().Be(DefGreater(Symmetric(b, 0.1), Symmetric(a, 0.1)).IsSatisfied());
    }

    [Theory]
    [InlineData(9.0, 10.0)]
    [InlineData(11.0, 10.0)]
    public void UpperBoundsLessThan_MatchesLowerBoundsGreaterThanWithSwappedArgs(double a, double b)
    {
        UpperLess(Symmetric(a, 0.1), Symmetric(b, 0.1)).IsSatisfied()
            .Should().Be(LowerGreater(Symmetric(b, 0.1), Symmetric(a, 0.1)).IsSatisfied());
    }

    [Theory]
    [InlineData(5.0, 10.0)]
    [InlineData(10.0, 5.0)]
    [InlineData(10.0, 10.0)]
    public void NominallyLessThan_MatchesNominallyGreaterThanWithSwappedArgs(double a, double b)
    {
        NomLess(Symmetric(a, 0.1), Symmetric(b, 0.1)).IsSatisfied()
            .Should().Be(NomGreater(Symmetric(b, 0.1), Symmetric(a, 0.1)).IsSatisfied());
    }
}
