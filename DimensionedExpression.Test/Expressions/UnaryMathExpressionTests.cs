using System;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Traversal;
using FluentAssertions;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Exceptions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.Expressions;

public class UnaryMathExpressionTests
{
    private static readonly Dimensionality Area = Dimensionality.Length * 2;

    private static Variable Dimensionless(double value, double relativeError) =>
        new("x", Dimensionality.Dimensionless.Quantity(value).Measurand(SymmetricUncertainty.FromRelErr(relativeError)));

    private static Variable UnboundDimensionless() =>
        new("x", Dimensionality.Dimensionless);

    private static Variable BoundArea(double squareMeters, double relativeError) =>
        new("a", Area.Quantity(squareMeters).Measurand(SymmetricUncertainty.FromRelErr(relativeError)));

    // ---- SqrtExpression ----

    [Fact]
    public void Sqrt_HalvesDimensionExponents()
    {
        new SqrtExpression(BoundArea(9, 0)).Dimensionality.Should().Be(Dimensionality.Length);
    }

    [Fact]
    public void Sqrt_ComputesRootAndHalvesRelativeError()
    {
        var root = new SqrtExpression(BoundArea(9, 0.02)).Value!;
        root.KmsValue.Should().BeApproximately(3, 1E-9);
        root.RelativeError.Should().BeApproximately(0.01, 1E-9);
    }

    [Fact]
    public void Sqrt_OddExponent_ThrowsOnDimensionAccess()
    {
        var sqrt = new SqrtExpression(new Variable("l", Dimensionality.Length));
        Func<Dimensionality> access = () => sqrt.Dimensionality;
        access.Should().Throw<NondiscreteDimensionalityException>();
    }

    [Fact]
    public void Sqrt_Unbound_IsNullAndPropagatesDoF()
    {
        var sqrt = new SqrtExpression(new Variable("a", Area));
        sqrt.IsFullyDescribed.Should().BeFalse();
        sqrt.Value.Should().BeNull();
        sqrt.FreeVariables().Should().HaveCount(1);
    }

    // ---- ExponentialExpression ----

    [Fact]
    public void Exp_IsDimensionless()
    {
        new ExponentialExpression(Dimensionless(2, 0)).Dimensionality.Should().Be(Dimensionality.Dimensionless);
    }

    [Fact]
    public void Exp_ComputesExpAndPropagatesRelativeError()
    {
        var result = new ExponentialExpression(Dimensionless(2, 0.01)).Value!;
        result.KmsValue.Should().BeApproximately(Math.Exp(2), 1E-9);
        result.RelativeError.Should().BeApproximately(0.02, 1E-9); // |x| * relErr(x)
    }

    [Fact]
    public void Exp_RejectsNonDimensionlessArgument()
    {
        Action construct = () => new ExponentialExpression(new Variable("a", Area));
        construct.Should().Throw<IncompatibleDimensionsException>();
    }

    [Fact]
    public void Exp_Unbound_IsNullAndPropagatesDoF()
    {
        var exp = new ExponentialExpression(UnboundDimensionless());
        exp.Value.Should().BeNull();
        exp.FreeVariables().Should().HaveCount(1);
    }

    // ---- NaturalLogExpression ----

    [Fact]
    public void Ln_IsDimensionless()
    {
        new NaturalLogExpression(Dimensionless(Math.E, 0)).Dimensionality.Should().Be(Dimensionality.Dimensionless);
    }

    [Fact]
    public void Ln_ComputesLogWithAbsoluteError()
    {
        // ln(e) = 1, AbsoluteError(ln x) ≈ RelativeError(x) = 0.1 → result RelativeError = 0.1 / |1|
        var result = new NaturalLogExpression(Dimensionless(Math.E, 0.1)).Value!;
        result.KmsValue.Should().BeApproximately(1, 1E-9);
        result.RelativeError.Should().BeApproximately(0.1, 1E-9);
    }

    [Fact]
    public void Ln_AbsoluteErrorScalesInverselyWithResult()
    {
        // ln(e²) = 2, absolute error 0.1 → relative error 0.1 / 2
        var result = new NaturalLogExpression(Dimensionless(Math.Exp(2), 0.1)).Value!;
        result.KmsValue.Should().BeApproximately(2, 1E-9);
        result.RelativeError.Should().BeApproximately(0.05, 1E-9);
    }

    [Fact]
    public void Ln_RejectsNonDimensionlessArgument()
    {
        Action construct = () => new NaturalLogExpression(new Variable("a", Area));
        construct.Should().Throw<IncompatibleDimensionsException>();
    }

    [Fact]
    public void Ln_AtOne_ProducesZeroValueWithAbsoluteError()
    {
        // ln(1) = 0. The absolute error (= RelativeError(x) = 0.05) is preserved as an absolute error; the
        // relative error of a zero-valued result is undefined (+inf) but no longer throws.
        var result = new NaturalLogExpression(Dimensionless(1, 0.05)).Value!;
        result.KmsValue.Should().Be(0);
        result.KmsAbsoluteError.Should().BeApproximately(0.05, 1E-9);
        double.IsPositiveInfinity(result.RelativeError).Should().BeTrue();
    }

    [Fact]
    public void Ln_Unbound_IsNullAndPropagatesDoF()
    {
        var ln = new NaturalLogExpression(UnboundDimensionless());
        ln.Value.Should().BeNull();
        ln.FreeVariables().Should().HaveCount(1);
    }
}
