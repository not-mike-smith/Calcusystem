using Calcusystem.Analysis.Extensions;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Analysis.Test;

/* Spec A §4.2 and acceptance 4: Flatten never counts a fixed value as an unknown, and Calculate treats it as
   valued. Fixed values are built from snapshots, the only way in from outside their assembly. */
public class FixedValueAnalysisTests
{
    private static readonly Dimensionality _acceleration =
        Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

    private static Constant Gravity() =>
        Constant.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.Constant,
            "g",
            "g",
            new Quantity(9.80665, _acceleration).WithoutUncertainty().GetSnapshot(),
            null));

    private static ReferenceValue Density() =>
        ReferenceValue.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.ReferenceValue,
            "rho",
            "ρ",
            new Quantity(1000, Dimensionality.Mass / (Dimensionality.Length * Dimensionality.Length * Dimensionality.Length))
                .Measurand(SymmetricUncertainty.FromRelative(0.01)).GetSnapshot(),
            null));

    private static Scalar Two() =>
        Scalar.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.Scalar,
            "two",
            null,
            new Quantity(2, Dimensionality.Dimensionless).WithoutUncertainty().GetSnapshot(),
            null));

    /* ½·v² = g·h, written as v² == 2·g·h with h unset. One unknown, one equation: the fixed values are neither. */
    [Fact]
    public void AFixedValueIsNeverAnUnknown()
    {
        var h = new Variable("h", Dimensionality.Length);
        var vSquared = new Variable("v²", _acceleration * Dimensionality.Length);
        var twoGh = new ProductExpression([Two(), Gravity(), h]);

        var system = ExpressionSystem.Create("free fall");
        system.Add(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Equation) { Id = "eq", Lhs = vSquared, Rhs = twoGh });

        var flat = system.Flatten();

        flat.Unknowns.Should().BeEquivalentTo([h, vSquared]);
        flat.Equations.Single().Unknowns.Should().BeEquivalentTo([h, vSquared]);
    }

    [Fact]
    public void ASystemOfOnlyFixedValuesHasNoUnknownsAndIsComplete()
    {
        var pressurePerMeter = new ProductExpression([Density(), Gravity()]);
        var system = ExpressionSystem.Create("ρ·g");
        system.Add(pressurePerMeter);

        system.Flatten().Unknowns.Should().BeEmpty();

        var calculation = system.Calculate();

        calculation.IsComplete.Should().BeTrue();
        calculation.MissingValues.Should().BeEmpty();
        calculation.ValueOf(pressurePerMeter)!.KmsValue.Should().BeApproximately(9806.65, 1e-9);
        calculation.ValueOf(pressurePerMeter)!.RelativeUncertainty.Should().BeApproximately(0.01, 1e-12);
    }

    [Fact]
    public void ACalculationHoldsEveryFixedValuesOwnValue()
    {
        var g = Gravity();
        var two = Two();
        var h = new Variable("h", Dimensionality.Length);
        var system = ExpressionSystem.Create("values");
        system.Add(new ProductExpression([two, g, h]));

        var calculation = system.Calculate();

        calculation.ValueOf(g)!.KmsValue.Should().Be(9.80665);
        calculation.ValueOf(two)!.KmsValue.Should().Be(2);
        calculation.MissingValues.Should().Equal(h);
        calculation.Unresolved.Should().ContainSingle().Which.Should().BeOfType<ProductExpression>();
    }
}
