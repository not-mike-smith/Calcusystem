using System;
using FluentAssertions;
using Measurement.Models;
using Measurement.Uncertainty;
using Measurement.Units;
using Xunit;

namespace Measurement.Test;

public class MeasurandTests
{
    private static Measurand Meters(double value, double relativeError = 0) =>
        Length.Meter.Quantity(value).Measurand(GaussianUncertainty.FromRelErr(relativeError));

    private static Measurand Kilograms(double value, double relativeError = 0) =>
        Mass.Kilogram.Quantity(value).Measurand(GaussianUncertainty.FromRelErr(relativeError));

    [Fact]
    public void TryAdd_HappyPath()
    {
        var a = Meters(1, 0.03);
        var b = Meters(2, 0.04);
        var sum = a.TryAdd(b);
        sum.Dimensionality.Should().Be(Dimensionality.Length);
        sum.KmsValue.Should().BeApproximately(3, 1E-9);
    }

    [Fact]
    public void TryAdd_NaNOnMismatch()
    {
        var length = Meters(1);
        var mass = Kilograms(1);
        var result = length.TryAdd(mass);
        result.IsNaN().Should().BeTrue();
    }

    [Fact]
    public void TrySubtract_HappyPath()
    {
        var a = Meters(3, 0.03);
        var b = Meters(1, 0.04);
        var diff = a.TrySubtract(b);
        diff.Dimensionality.Should().Be(Dimensionality.Length);
        diff.KmsValue.Should().BeApproximately(2, 1E-9);
    }

    [Fact]
    public void TrySubtract_NaNOnMismatch()
    {
        var length = Meters(1);
        var mass = Kilograms(1);
        var result = length.TrySubtract(mass);
        result.IsNaN().Should().BeTrue();
    }

    [Fact]
    public void Minus_CancelingToZero_KeepsAbsoluteErrorWithoutThrowing()
    {
        // 2m ± 0.02 (abs) minus itself → value 0. The propagated error is stored as an absolute error rather
        // than dividing by the (zero) sum, so this no longer throws.
        var difference = Meters(2, 0.01).Minus(Meters(2, 0.01));

        difference.KmsValue.Should().Be(0);
        difference.KmsAbsoluteError.Should().BeApproximately(Math.Sqrt(2) * 0.02, 1E-9);
        double.IsPositiveInfinity(difference.RelativeError).Should().BeTrue();
    }

    [Fact]
    public void FromAbsErr_OnZeroValue_KeepsAbsoluteErrorWithoutThrowing()
    {
        var zero = Length.Meter.Quantity(0).Measurand(GaussianUncertainty.FromAbsErr(Length.Meter.Quantity(0.5)));

        zero.KmsValue.Should().Be(0);
        zero.KmsAbsoluteError.Should().BeApproximately(0.5, 1E-9);
        double.IsPositiveInfinity(zero.RelativeError).Should().BeTrue();
    }

    [Fact]
    public void ToPower_ScalesRelativeErrorByExponent()
    {
        Meters(2, 0.01).ToPower(2).RelativeError.Should().BeApproximately(0.02, 1E-9);
    }

    [Fact]
    public void ToRoot_ScalesRelativeErrorByReciprocalOfRoot()
    {
        // area (L²) so the square root yields an integer-exponent dimension
        var area = (Dimensionality.Length * 2).Quantity(4).Measurand(GaussianUncertainty.FromRelErr(0.02));
        area.ToRoot(2).RelativeError.Should().BeApproximately(0.01, 1E-9);
    }

    [Fact]
    public void ToPower_PreservesAsymmetry()
    {
        // upper 1%, lower 2%; squaring scales both by |2|, keeping them distinct
        var m = Length.Meter.Quantity(2).Measurand(AsymmetricUncertainty.FromRelErr(0.01, 0.02));

        var squared = m.ToPower(2);
        squared.UpperRelativeError.Should().BeApproximately(0.02, 1E-9);
        squared.LowerRelativeError.Should().BeApproximately(0.04, 1E-9);
    }
}
