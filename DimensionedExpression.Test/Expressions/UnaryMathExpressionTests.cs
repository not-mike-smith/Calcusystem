using System;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.Measurement.Exceptions;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.Expressions;

public class UnaryMathExpressionTests
{
    private static readonly Dimensionality Area = Dimensionality.Length * 2;

    private static Variable Dimensionless(double value, double relativeUncertainty) =>
        new("x", Dimensionality.Dimensionless.Quantity(value).Measurand(SymmetricUncertainty.FromRelative(relativeUncertainty)));

    private static Variable UnsetDimensionless() =>
        new("x", Dimensionality.Dimensionless);

    private static Variable BoundArea(double squareMeters, double relativeUncertainty) =>
        new("a", Area.Quantity(squareMeters).Measurand(SymmetricUncertainty.FromRelative(relativeUncertainty)));

    // ---- SqrtExpression ----

    [Fact]
    public void Sqrt_HalvesDimensionExponents()
    {
        new SqrtExpression(BoundArea(9, 0)).Dimensionality.Should().Be(Dimensionality.Length);
    }

    [Fact]
    public void Sqrt_ComputesRootAndHalvesRelativeUncertainty()
    {
        var root = new SqrtExpression(BoundArea(9, 0.02)).ComputeIfFullyDescribed()!;
        root.KmsValue.Should().BeApproximately(3, 1E-9);
        root.RelativeUncertainty.Should().BeApproximately(0.01, 1E-9);
    }

    [Fact]
    public void Sqrt_OddExponent_ThrowsOnDimensionAccess()
    {
        var sqrt = new SqrtExpression(new Variable("l", Dimensionality.Length));
        Func<Dimensionality> access = () => sqrt.Dimensionality;
        access.Should().Throw<NondiscreteDimensionalityException>();
    }

    [Fact]
    public void Sqrt_Unset_IsNullAndPropagatesDoF()
    {
        var sqrt = new SqrtExpression(new Variable("a", Area));
        sqrt.IsFullyDescribed.Should().BeFalse();
        sqrt.ComputeIfFullyDescribed().Should().BeNull();
        sqrt.UnsetVariables().Should().HaveCount(1);
    }

    // ---- ExponentialExpression ----

    [Fact]
    public void Exp_IsDimensionless()
    {
        new ExponentialExpression(Dimensionless(2, 0)).Dimensionality.Should().Be(Dimensionality.Dimensionless);
    }

    [Fact]
    public void Exp_ComputesExpAndPropagatesRelativeUncertainty()
    {
        var result = new ExponentialExpression(Dimensionless(2, 0.01)).ComputeIfFullyDescribed()!;
        result.KmsValue.Should().BeApproximately(Math.Exp(2), 1E-9);
        result.RelativeUncertainty.Should().BeApproximately(0.02, 1E-9); // |x| * relErr(x)
    }

    [Fact]
    public void Exp_RejectsNonDimensionlessArgument()
    {
        Action construct = () => new ExponentialExpression(new Variable("a", Area));
        construct.Should().Throw<IncompatibleDimensionsException>();
    }

    [Fact]
    public void Exp_Unset_IsNullAndPropagatesDoF()
    {
        var exp = new ExponentialExpression(UnsetDimensionless());
        exp.ComputeIfFullyDescribed().Should().BeNull();
        exp.UnsetVariables().Should().HaveCount(1);
    }

    // ---- NaturalLogExpression ----

    [Fact]
    public void Ln_IsDimensionless()
    {
        new NaturalLogExpression(Dimensionless(Math.E, 0)).Dimensionality.Should().Be(Dimensionality.Dimensionless);
    }

    [Fact]
    public void Ln_ComputesLogWithAbsoluteUncertainty()
    {
        // ln(e) = 1, AbsoluteUncertainty(ln x) ≈ RelativeUncertainty(x) = 0.1 → result RelativeUncertainty = 0.1 / |1|
        var result = new NaturalLogExpression(Dimensionless(Math.E, 0.1)).ComputeIfFullyDescribed()!;
        result.KmsValue.Should().BeApproximately(1, 1E-9);
        result.RelativeUncertainty.Should().BeApproximately(0.1, 1E-9);
    }

    [Fact]
    public void Ln_AbsoluteUncertaintyScalesInverselyWithResult()
    {
        // ln(e²) = 2, absolute error 0.1 → relative error 0.1 / 2
        var result = new NaturalLogExpression(Dimensionless(Math.Exp(2), 0.1)).ComputeIfFullyDescribed()!;
        result.KmsValue.Should().BeApproximately(2, 1E-9);
        result.RelativeUncertainty.Should().BeApproximately(0.05, 1E-9);
    }

    [Fact]
    public void Ln_RejectsNonDimensionlessArgument()
    {
        Action construct = () => new NaturalLogExpression(new Variable("a", Area));
        construct.Should().Throw<IncompatibleDimensionsException>();
    }

    [Fact]
    public void Ln_AtOne_ProducesZeroValueWithAbsoluteUncertainty()
    {
        // ln(1) = 0. The absolute error (= RelativeUncertainty(x) = 0.05) is preserved as an absolute error; the
        // relative error of a zero-valued result is undefined (+inf) but no longer throws.
        var result = new NaturalLogExpression(Dimensionless(1, 0.05)).ComputeIfFullyDescribed()!;
        result.KmsValue.Should().Be(0);
        result.KmsAbsoluteUncertainty.Should().BeApproximately(0.05, 1E-9);
        double.IsPositiveInfinity(result.RelativeUncertainty).Should().BeTrue();
    }

    [Fact]
    public void Ln_Unset_IsNullAndPropagatesDoF()
    {
        var ln = new NaturalLogExpression(UnsetDimensionless());
        ln.ComputeIfFullyDescribed().Should().BeNull();
        ln.UnsetVariables().Should().HaveCount(1);
    }
}
