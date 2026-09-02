using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.Expressions;

/// <summary>
/// <c>UnsetVariables()</c> is what a node contributes to a system's unknowns, and the count of it replaced the
/// per-type <c>DegreesOfFreedom()</c> each node used to hand-roll over its own child collection.
/// </summary>
public class UnsetVariablesTests
{
    private static readonly Dimensionality Mass = Measurement.Units.Mass.Kilogram.Dimensionality;
    private static readonly Dimensionality Length = Measurement.Units.Length.Meter.Dimensionality;

    private static Variable Unset(Dimensionality dim) =>
        new("x", dim);

    private static Variable Valued(double kgValue) =>
        new("x", Measurement.Units.Mass.Kilogram.Quantity(kgValue).Measurand(SymmetricUncertainty.FromRelative(0)));

    [Fact]
    public void UnsetDirectVariable_IsItsOwnFreeVariable()
    {
        var x = Unset(Mass);
        x.UnsetVariables().Should().Equal(x);
    }

    [Fact]
    public void BoundDirectVariable_IsNotFree()
    {
        Valued(5).UnsetVariables().Should().BeEmpty();
    }

    [Fact]
    public void NegatedUnset_PropagatesFreeVariable()
    {
        new NegatedExpression(Unset(Mass)).UnsetVariables().Should().HaveCount(1);
    }

    [Fact]
    public void NegatedValued_HasNoUnsetVariables()
    {
        new NegatedExpression(Valued(5)).UnsetVariables().Should().BeEmpty();
    }

    [Fact]
    public void ReciprocalUnset_PropagatesFreeVariable()
    {
        new ReciprocalExpression(Unset(Mass)).UnsetVariables().Should().HaveCount(1);
    }

    [Fact]
    public void ReciprocalValued_HasNoUnsetVariables()
    {
        new ReciprocalExpression(Valued(5)).UnsetVariables().Should().BeEmpty();
    }

    [Fact]
    public void ProductExpression_TwoUnsetFactors_HasTwoUnsetVariables()
    {
        var product = new ProductExpression([Unset(Mass), Unset(Length)]);
        product.UnsetVariables().Should().HaveCount(2);
    }

    [Fact]
    public void ProductExpression_AllValued_HasNoUnsetVariables()
    {
        var product = new ProductExpression([Valued(5), Valued(3)]);
        product.UnsetVariables().Should().BeEmpty();
    }

    [Fact]
    public void ProductExpression_MixedBoundedness_YieldsOnlyUnset()
    {
        var unset = Unset(Length);
        var product = new ProductExpression([Valued(5), unset]);
        product.UnsetVariables().Should().Equal(unset);
    }

    [Fact]
    public void SumExpression_TwoUnset_HasTwoUnsetVariables()
    {
        var sum = new SumExpression([Unset(Mass), Unset(Mass)]);
        sum.UnsetVariables().Should().HaveCount(2);
    }

    [Fact]
    public void SumExpression_OneBoundOneUnset_HasOneFreeVariable()
    {
        var sum = new SumExpression([Valued(3), Unset(Mass)]);
        sum.UnsetVariables().Should().HaveCount(1);
    }

    [Fact]
    public void QuotientExpression_UnsetNumerator_BoundDenominator_HasOneFreeVariable()
    {
        var quotient = new QuotientExpression
        {
            Id = "test",
            Numerator = Unset(Mass),
            Denominator = Valued(2)
        };
        quotient.UnsetVariables().Should().HaveCount(1);
    }

    [Fact]
    public void QuotientExpression_BothValued_HasNoUnsetVariables()
    {
        var quotient = new QuotientExpression
        {
            Id = "test",
            Numerator = Valued(10),
            Denominator = Valued(2)
        };
        quotient.UnsetVariables().Should().BeEmpty();
    }

    [Fact]
    public void QuotientExpression_BothUnset_HasTwoUnsetVariables()
    {
        var quotient = new QuotientExpression
        {
            Id = "test",
            Numerator = Unset(Mass),
            Denominator = Unset(Mass)
        };
        quotient.UnsetVariables().Should().HaveCount(2);
    }

    [Fact]
    public void NestedProductExpression_RecursivelyCollectsAllUnset()
    {
        // (a * b) * c → three unknowns
        var inner = new ProductExpression([Unset(Mass), Unset(Mass)]);
        var outer = new ProductExpression([inner, Unset(Mass)]);
        outer.UnsetVariables().Should().HaveCount(3);
    }

    [Fact]
    public void NestedExpression_PartiallyValued_CollectsCorrectly()
    {
        // (a * 5kg) / b, where a is unset → two unknowns
        var numerator = new ProductExpression([Unset(Mass), Valued(5)]);
        var quotient = new QuotientExpression
        {
            Id = "test",
            Numerator = numerator,
            Denominator = Unset(Mass)
        };

        quotient.UnsetVariables().Should().HaveCount(2);
    }

    // ── Sharing ──────────────────────────────────────────────────────────────
    // The graph is a DAG: one node may be referenced from several parents. The per-type DegreesOfFreedom() this
    // replaced summed over children, so a shared unknown was counted once per reference — a system with one
    // unknown reported two, and would have been misclassified as underdetermined by the DoF gate.

    [Fact]
    public void SharedUnsetVariable_IsCountedOnce()
    {
        // m * m — one unknown, referenced twice.
        var m = Unset(Mass);
        var product = new ProductExpression([m, m]);
        product.UnsetVariables().Should().Equal(m);
    }

    [Fact]
    public void UnknownSharedAcrossDistinctSubexpressions_IsCountedOnce()
    {
        // (m * a) / (m + b): m reaches the root by two different paths.
        var m = Unset(Mass);
        var a = Unset(Mass);
        var b = Unset(Mass);

        var numerator = new ProductExpression([m, a]);
        var denominator = new SumExpression([m, b]);
        var quotient = new QuotientExpression
        {
            Id = "test", Numerator = numerator, Denominator = denominator
        };

        quotient.UnsetVariables().Should().HaveCount(3);
        quotient.UnsetVariables().Should().BeEquivalentTo(new[] { m, a, b });
    }

    [Fact]
    public void SharedComputedSubexpression_IsWalkedOnce()
    {
        // s = (a + b); s * s → still just two unknowns.
        var a = Unset(Mass);
        var b = Unset(Mass);
        var sum = new SumExpression([a, b]);
        var product = new ProductExpression([sum, sum]);
        product.UnsetVariables().Should().HaveCount(2);
        product.SelfAndDescendants().Should().HaveCount(4); // product, sum, a, b
    }
}
