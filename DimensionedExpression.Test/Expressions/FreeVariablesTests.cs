using DimensionedExpression.Expressions;
using DimensionedExpression.Traversal;
using FluentAssertions;
using Measurement;
using Measurement.Units;
using Xunit;

namespace DimensionedExpression.Test.Expressions;

/// <summary>
/// <c>FreeVariables()</c> is what a node contributes to a system's unknowns, and the count of it replaced the
/// per-type <c>DegreesOfFreedom()</c> each node used to hand-roll over its own child collection.
/// </summary>
public class FreeVariablesTests
{
    private static readonly Dimensionality Mass = Measurement.Units.Mass.Kilogram.Dimensionality;
    private static readonly Dimensionality Length = Measurement.Units.Length.Meter.Dimensionality;

    private static Variable Unbound(Dimensionality dim) =>
        new("x", dim);

    private static Variable Bound(double kgValue) =>
        new("x", Measurement.Units.Mass.Kilogram.Quantity(kgValue).Measurand(SymmetricUncertainty.FromRelErr(0)));

    [Fact]
    public void UnboundDirectVariable_IsItsOwnFreeVariable()
    {
        var x = Unbound(Mass);
        x.FreeVariables().Should().Equal(x);
    }

    [Fact]
    public void BoundDirectVariable_IsNotFree()
    {
        Bound(5).FreeVariables().Should().BeEmpty();
    }

    [Fact]
    public void NegatedUnbound_PropagatesFreeVariable()
    {
        new NegatedExpression(Unbound(Mass)).FreeVariables().Should().HaveCount(1);
    }

    [Fact]
    public void NegatedBound_HasNoFreeVariables()
    {
        new NegatedExpression(Bound(5)).FreeVariables().Should().BeEmpty();
    }

    [Fact]
    public void ReciprocalUnbound_PropagatesFreeVariable()
    {
        new ReciprocalExpression(Unbound(Mass)).FreeVariables().Should().HaveCount(1);
    }

    [Fact]
    public void ReciprocalBound_HasNoFreeVariables()
    {
        new ReciprocalExpression(Bound(5)).FreeVariables().Should().BeEmpty();
    }

    [Fact]
    public void ProductExpression_TwoUnboundFactors_HasTwoFreeVariables()
    {
        var product = new ProductExpression();
        product.AddFactor(Unbound(Mass));
        product.AddFactor(Unbound(Length));
        product.FreeVariables().Should().HaveCount(2);
    }

    [Fact]
    public void ProductExpression_AllBound_HasNoFreeVariables()
    {
        var product = new ProductExpression();
        product.AddFactor(Bound(5));
        product.AddFactor(Bound(3));
        product.FreeVariables().Should().BeEmpty();
    }

    [Fact]
    public void ProductExpression_MixedBoundedness_YieldsOnlyUnbound()
    {
        var unbound = Unbound(Length);
        var product = new ProductExpression();
        product.AddFactor(Bound(5));
        product.AddFactor(unbound);
        product.FreeVariables().Should().Equal(unbound);
    }

    [Fact]
    public void SumExpression_TwoUnbound_HasTwoFreeVariables()
    {
        var sum = new SumExpression(Mass);
        sum.AddAddend(Unbound(Mass));
        sum.AddAddend(Unbound(Mass));
        sum.FreeVariables().Should().HaveCount(2);
    }

    [Fact]
    public void SumExpression_OneBoundOneUnbound_HasOneFreeVariable()
    {
        var sum = new SumExpression(Mass);
        sum.AddAddend(Bound(3));
        sum.AddAddend(Unbound(Mass));
        sum.FreeVariables().Should().HaveCount(1);
    }

    [Fact]
    public void QuotientExpression_UnboundNumerator_BoundDenominator_HasOneFreeVariable()
    {
        var quotient = new QuotientExpression
        {
            Id = "test",
            Numerator = Unbound(Mass),
            Denominator = Bound(2)
        };
        quotient.FreeVariables().Should().HaveCount(1);
    }

    [Fact]
    public void QuotientExpression_BothBound_HasNoFreeVariables()
    {
        var quotient = new QuotientExpression
        {
            Id = "test",
            Numerator = Bound(10),
            Denominator = Bound(2)
        };
        quotient.FreeVariables().Should().BeEmpty();
    }

    [Fact]
    public void QuotientExpression_BothUnbound_HasTwoFreeVariables()
    {
        var quotient = new QuotientExpression
        {
            Id = "test",
            Numerator = Unbound(Mass),
            Denominator = Unbound(Mass)
        };
        quotient.FreeVariables().Should().HaveCount(2);
    }

    [Fact]
    public void NestedProductExpression_RecursivelyCollectsAllUnbound()
    {
        // (a * b) * c → three unknowns
        var inner = new ProductExpression();
        inner.AddFactor(Unbound(Mass));
        inner.AddFactor(Unbound(Mass));

        var outer = new ProductExpression();
        outer.AddFactor(inner);
        outer.AddFactor(Unbound(Mass));

        outer.FreeVariables().Should().HaveCount(3);
    }

    [Fact]
    public void NestedExpression_PartiallyBound_CollectsCorrectly()
    {
        // (a * 5kg) / b, where a is unbound → two unknowns
        var numerator = new ProductExpression();
        numerator.AddFactor(Unbound(Mass));
        numerator.AddFactor(Bound(5));

        var quotient = new QuotientExpression
        {
            Id = "test",
            Numerator = numerator,
            Denominator = Unbound(Mass)
        };

        quotient.FreeVariables().Should().HaveCount(2);
    }

    // ── Sharing ──────────────────────────────────────────────────────────────
    // The graph is a DAG: one node may be referenced from several parents. The per-type DegreesOfFreedom() this
    // replaced summed over children, so a shared unknown was counted once per reference — a system with one
    // unknown reported two, and would have been misclassified as underdetermined by the DoF gate.

    [Fact]
    public void SharedUnboundVariable_IsCountedOnce()
    {
        // m * m — one unknown, referenced twice.
        var m = Unbound(Mass);
        var product = new ProductExpression();
        product.AddFactor(m);
        product.AddFactor(m);

        product.FreeVariables().Should().Equal(m);
    }

    [Fact]
    public void UnknownSharedAcrossDistinctSubexpressions_IsCountedOnce()
    {
        // (m * a) / (m + b): m reaches the root by two different paths.
        var m = Unbound(Mass);
        var a = Unbound(Mass);
        var b = Unbound(Mass);

        var numerator = new ProductExpression();
        numerator.AddFactor(m);
        numerator.AddFactor(a);

        var denominator = new SumExpression(Mass);
        denominator.AddAddend(m);
        denominator.AddAddend(b);

        var quotient = new QuotientExpression
        {
            Id = "test", Numerator = numerator, Denominator = denominator
        };

        quotient.FreeVariables().Should().HaveCount(3);
        quotient.FreeVariables().Should().BeEquivalentTo(new[] { m, a, b });
    }

    [Fact]
    public void SharedComputedSubexpression_IsWalkedOnce()
    {
        // s = (a + b); s * s → still just two unknowns.
        var a = Unbound(Mass);
        var b = Unbound(Mass);
        var sum = new SumExpression(Mass);
        sum.AddAddend(a);
        sum.AddAddend(b);

        var product = new ProductExpression();
        product.AddFactor(sum);
        product.AddFactor(sum);

        product.FreeVariables().Should().HaveCount(2);
        product.SelfAndDescendants().Should().HaveCount(4); // product, sum, a, b
    }
}
