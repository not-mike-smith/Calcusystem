using Calcusystem.Analysis.Enums;
using Calcusystem.Analysis.Extensions;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Analysis.Test;

/* The snippets printed in the READMEs, compiled. Documentation that no longer builds is worse than none, and
   prose alone cannot show that a shown `using` list is sufficient.

   The analysis half of the root README lands here because Measurement.Test.ReadmeSnippets cannot reach it: that
   project references only Measurement, by design. */
public class ReadmeSnippets
{
    private static Variable Valued(string symbol, double kmsValue, Dimensionality dimensionality) =>
        new(symbol,
            new Quantity(kmsValue, dimensionality).Measurand(SymmetricUncertainty.FromRelative(0.01)),
            symbol);

    [Fact]
    public void RootReadmeQuickStart()
    {
        var acceleration = Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);
        var m = Valued("m", 2, Dimensionality.Mass);
        var a = Valued("a", 3, acceleration);
        var f = new ProductExpression([m, a]) { Id = "f" };

        var system = ExpressionSystem.Create("Newton's second law", "F = m·a");
        system.Add(f);

        var flat = system.Flatten();
        flat.DegreesOfFreedom.Should().Be(0);
        flat.Determination.Should().Be(Determination.ExactlyDetermined);

        var parts = flat.Decompose();
        parts.IsWellPosed.Should().BeTrue();
        parts.WellDetermined.IsEmpty.Should().BeTrue("nothing is outstanding to determine");
        parts.Underdetermined.IsEmpty.Should().BeTrue();

        var calc = system.Calculate();
        calc.ValueOf(f)!.KmsValue.Should().BeApproximately(6, 1e-12);
        calc.MissingValues.Should().BeEmpty();
    }

    /* The worked table in the Analysis README: four unknowns, four equations, and a decomposition that is not
       well posed despite the count landing on zero. */
    [Fact]
    public void AnalysisReadmeDecomposition()
    {
        EqualityOperator Equation(string id, IExpression lhs, IExpression rhs) =>
            new(AgreementRule.Nominal, SolvingRole.Equation) { Id = id, Lhs = lhs, Rhs = rhs };

        Variable Unknown(string symbol) => new(symbol, Dimensionality.Mass, symbol);

        var orphan = Unknown("orphan");
        var m = Unknown("m");
        var x = Unknown("x");
        var y = Unknown("y");

        var system = ExpressionSystem.Create("worked table", "");
        system.Add(orphan);
        system.Add(Equation("m==a", m, Valued("a", 5, Dimensionality.Mass)));
        system.Add(Equation("m==b", m, Valued("b", 5, Dimensionality.Mass)));
        system.Add(Equation("x==3", x, Valued("3kg", 3, Dimensionality.Mass)));
        system.Add(Equation(
            "y==x+1",
            y,
            new SumExpression([x, Valued("1kg", 1, Dimensionality.Mass)]) { Id = "x+1" }));

        var flat = system.Flatten();
        var decomposition = flat.Decompose();

        decomposition.Underdetermined.DegreesOfFreedom.Should().Be(1);
        decomposition.WellDetermined.DegreesOfFreedom.Should().Be(0);
        decomposition.Overdetermined.DegreesOfFreedom.Should().Be(-1);

        flat.DegreesOfFreedom.Should().Be(0, "the parts sum to the aggregate");
        decomposition.IsWellPosed.Should().BeFalse();

        decomposition.DeterminableUnknowns.Select(u => u.Id).Should().BeEquivalentTo("m", "x", "y");
    }
}
