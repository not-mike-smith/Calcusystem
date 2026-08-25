using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.Expressions;

/// <summary>
/// <c>ComputeFrom</c> returns <c>Measurand?</c>, and null is its answer for "not determinable" — a leaf with no
/// value says so that way. A composite handed an incomplete map has the same answer available, and used to throw
/// <c>KeyNotFoundException</c> instead: a public method whose nullable return already means "cannot tell".
/// </summary>
public class ComputeFromTests
{
    private static Variable Bound(string symbol, double kms) =>
        new(symbol, new Quantity(kms, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelErr(0)), symbol);

    private static readonly IReadOnlyDictionary<IExpression, Measurand> Nothing =
        new Dictionary<IExpression, Measurand>();

    public static TheoryData<string, IExpression> Composites()
    {
        var a = Bound("a", 2);
        var b = Bound("b", 3);

        var product = new ProductExpression([a, b]) { Id = "p" };
        var sum = new SumExpression([a, b]) { Id = "s" };
        return new TheoryData<string, IExpression>
        {
            { "product", product },
            { "sum", sum },
            { "quotient", new QuotientExpression { Id = "q", Numerator = a, Denominator = b } },
            { "negated", new NegatedExpression(a) { Id = "n" } },
            { "reciprocal", new ReciprocalExpression(a) { Id = "r" } },
        };
    }

    [Theory]
    [MemberData(nameof(Composites))]
    public void AnIncompleteMapYieldsNullRatherThanThrowing(string _, IExpression composite)
    {
        var act = () => composite.ComputeFrom(Nothing);

        act.Should().NotThrow();
        composite.ComputeFrom(Nothing).Should().BeNull();
    }

    [Fact]
    public void APartiallyCompleteMapAlsoYieldsNull()
    {
        // The half-supplied case, which a `Count` check alone would miss.
        var a = Bound("a", 2);
        var b = Bound("b", 3);
        var quotient = new QuotientExpression { Id = "q", Numerator = a, Denominator = b };

        var onlyNumerator = new Dictionary<IExpression, Measurand> { [a] = a.Value! };

        quotient.ComputeFrom(onlyNumerator).Should().BeNull();
    }

    [Fact]
    public void AnUnboundLeafSaysSoTheSameWay()
    {
        new Variable("x", Dimensionality.Mass, "x").ComputeFrom(Nothing).Should().BeNull();
    }

    [Fact]
    public void OverridesReachASingleNodeWithoutASystem()
    {
        // The node-level mirror of `Calculate`'s overrides: useful for working on one sub-expression, and for a
        // solver iterating over a subtree without building a system around it.
        var m = new Variable("m", Dimensionality.Mass, "m");
        var negated = new NegatedExpression(m) { Id = "n" };

        negated.ComputeIfDetermined().Should().BeNull();

        var trial = new Quantity(4, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelErr(0));
        negated.ComputeIfDetermined(new Dictionary<Variable, Measurand> { [m] = trial })!
            .KmsValue.Should().BeApproximately(-4, 1e-9);

        m.Value.Should().BeNull();
    }
}
