using Calcusystem.Analysis;
using DimensionedExpression.BinaryOperators;
using DimensionedExpression.Expressions;
using DimensionedExpression.Interfaces;
using DimensionedExpression.Systems;
using FluentAssertions;
using Measurement;
using Xunit;

namespace Calcusystem.Analysis.Test;

public class FlatSystemTests
{
    private static Variable Unbound(string symbol, Dimensionality? dim = null) =>
        new(symbol, dim ?? Dimensionality.Mass, symbol);

    private static Variable Bound(string symbol, double kmsValue, Dimensionality? dim = null) =>
        new(symbol,
            new Quantity(kmsValue, dim ?? Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelErr(0.01)),
            symbol);

    private static EqualityOperator Equation(string id, IExpression lhs, IExpression rhs) =>
        new(new AlwaysEqual(), isDetermining: true) { Id = id, Lhs = lhs, Rhs = rhs };

    // ── What lands in the flat system ────────────────────────────────────────

    /// <remarks>
    /// The worked example the design was settled on: variable <c>a</c>, <c>b = 1/a</c>, <c>b == c</c>, and
    /// <c>c == 2s</c>. Two unknowns, two equations — and <c>b</c>, a computed node, is neither. It appears only
    /// as the path by which the first equation reaches <c>a</c>.
    /// </remarks>
    [Fact]
    public void ComputedNodesAreNeitherUnknownsNorEquations()
    {
        var a = Unbound("a", Dimensionality.Time.Reciprocal());
        var b = new ReciprocalExpression(a) { Id = "b" };
        var c = Unbound("c", Dimensionality.Time);
        var twoSeconds = Bound("two_s", 2, Dimensionality.Time);

        var system = ExpressionSystem.Create("worked example", "");
        system.DirectExpressions.Add(a);
        system.DirectExpressions.Add(c);
        system.DirectExpressions.Add(twoSeconds);
        system.DerivedExpressions.Add(b);
        system.Relationships.Add(Equation("b==c", b, c));
        system.Relationships.Add(Equation("c==2s", c, twoSeconds));

        var flat = SystemFlattener.Flatten(system);

        flat.Unknowns.Select(u => u.Id).Should().BeEquivalentTo("a", "c");
        flat.Equations.Should().HaveCount(2);
        flat.DegreesOfFreedom.Should().Be(0);
        flat.Determination.Should().Be(Determination.ExactlyDetermined);

        // The incidence of `b == c` reaches `a` through b, and b itself is not a column.
        flat.Equations.Single(e => e.Relationship.Id == "b==c")
            .Unknowns.Select(u => u.Id).Should().BeEquivalentTo("a", "c");
    }

    /// <remarks>
    /// The same model expressed by valuing the leaf instead of asserting an equation. One fewer column and one
    /// fewer row, so degrees of freedom is unchanged — the two modelling styles cannot disagree about it.
    /// </remarks>
    [Fact]
    public void ValuingALeafAndAssertingAnEquationAgreeOnDegreesOfFreedom()
    {
        var a = Unbound("a", Dimensionality.Time.Reciprocal());
        var b = new ReciprocalExpression(a) { Id = "b" };
        var c = Bound("c", 2, Dimensionality.Time);

        var system = ExpressionSystem.Create("bound leaf", "");
        system.DirectExpressions.Add(a);
        system.DirectExpressions.Add(c);
        system.DerivedExpressions.Add(b);
        system.Relationships.Add(Equation("b==c", b, c));

        var flat = SystemFlattener.Flatten(system);

        flat.Unknowns.Select(u => u.Id).Should().Equal("a");
        flat.Equations.Should().HaveCount(1);
        flat.DegreesOfFreedom.Should().Be(0);
    }

    [Fact]
    public void VariablesReachableOnlyThroughDerivedExpressionsAreStillUnknowns()
    {
        var m = Unbound("m");
        var product = new ProductExpression { Id = "p" };
        product.AddFactor(m);
        product.AddFactor(Unbound("a"));

        var system = ExpressionSystem.Create("derived only", "");
        system.DerivedExpressions.Add(product);

        var flat = SystemFlattener.Flatten(system);

        flat.Unknowns.Select(u => u.Id).Should().BeEquivalentTo("m", "a");
        flat.Equations.Should().BeEmpty();
        flat.Determination.Should().Be(Determination.Underdetermined);
    }

    [Fact]
    public void AnUnknownSharedAcrossExpressionsIsOneColumn()
    {
        // m appears in a derived expression and on both sides of a relationship: still one unknown.
        var m = Unbound("m");
        var negated = new NegatedExpression(m) { Id = "neg" };

        var system = ExpressionSystem.Create("shared", "");
        system.DirectExpressions.Add(m);
        system.DerivedExpressions.Add(negated);
        system.Relationships.Add(Equation("eq", m, negated));

        var flat = SystemFlattener.Flatten(system);

        flat.Unknowns.Select(u => u.Id).Should().Equal("m");
        flat.Equations.Single().Unknowns.Select(u => u.Id).Should().Equal("m");
        flat.DegreesOfFreedom.Should().Be(0);
    }

    // ── Only determining relationships are equations ─────────────────────────

    [Fact]
    public void ConstraintsAreNotEquations()
    {
        var m = Unbound("m");
        var spec = Bound("spec", 5);

        var system = ExpressionSystem.Create("checks only", "");
        system.DirectExpressions.Add(m);
        system.DirectExpressions.Add(spec);
        system.Relationships.Add(new WithinBindingToleranceOperator { Id = "tol", Lhs = m, Rhs = spec });
        system.Relationships.Add(new DefinitelyLessThanOperator { Id = "lt", Lhs = m, Rhs = spec });
        system.Relationships.Add(new EqualityOperator(new AlwaysEqual(), isDetermining: false)
        {
            Id = "check", Lhs = m, Rhs = spec
        });

        var flat = SystemFlattener.Flatten(system);

        // Three relationships, none of which can determine a value: m stays unknown.
        flat.Equations.Should().BeEmpty();
        flat.DegreesOfFreedom.Should().Be(1);
        flat.Determination.Should().Be(Determination.Underdetermined);
    }

    /// <remarks>
    /// Whether a variable is an unknown depends on whether it has a value, not on what kind of relationship
    /// mentions it. A bound length under <c>l &lt; 3 m</c> is known and checkable; the same length unbound is
    /// still unknown, because a constraint bounds a value rather than producing one — and so it also appears in
    /// <c>UnknownsWithNoEquation</c> despite carrying a constraint.
    /// </remarks>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AConstraintNeverDeterminesItsSubject(bool lengthIsKnown)
    {
        var length = lengthIsKnown
            ? Bound("l", 2, Dimensionality.Length)
            : Unbound("l", Dimensionality.Length);
        var limit = Bound("3m", 3, Dimensionality.Length);

        var system = ExpressionSystem.Create("bounded length", "");
        system.DirectExpressions.Add(length);
        system.DirectExpressions.Add(limit);
        system.Relationships.Add(new DefinitelyLessThanOperator { Id = "l<3m", Lhs = length, Rhs = limit });

        var flat = SystemFlattener.Flatten(system);

        flat.Equations.Should().BeEmpty();

        if (lengthIsKnown)
        {
            flat.Unknowns.Should().BeEmpty();
            flat.Determination.Should().Be(Determination.ExactlyDetermined);
            flat.UnknownsWithNoEquation.Should().BeEmpty();
        }
        else
        {
            flat.Unknowns.Select(u => u.Id).Should().Equal("l");
            flat.Determination.Should().Be(Determination.Underdetermined);
            flat.UnknownsWithNoEquation.Select(u => u.Id).Should().Equal("l");
        }
    }

    // ── Classification ───────────────────────────────────────────────────────

    [Fact]
    public void MoreEquationsThanUnknownsIsOverdeterminedRatherThanAnError()
    {
        var m = Unbound("m");
        var a = Bound("a", 5);
        var b = Bound("b", 5);

        var system = ExpressionSystem.Create("redundant", "");
        system.DirectExpressions.Add(m);
        system.Relationships.Add(Equation("m==a", m, a));
        system.Relationships.Add(Equation("m==b", m, b));

        var flat = SystemFlattener.Flatten(system);

        flat.DegreesOfFreedom.Should().Be(-1);
        flat.Determination.Should().Be(Determination.Overdetermined);
        flat.Equations.Should().HaveCount(2);
    }

    [Fact]
    public void UnknownsWithNoEquationAreReportedSeparatelyFromTheCount()
    {
        // Square overall, but `orphan` has no equation on it and `m` has two — the aggregate hides both.
        var m = Unbound("m");
        var orphan = Unbound("orphan");
        var a = Bound("a", 5);
        var b = Bound("b", 5);

        var system = ExpressionSystem.Create("hidden singularity", "");
        system.DirectExpressions.Add(m);
        system.DirectExpressions.Add(orphan);
        system.Relationships.Add(Equation("m==a", m, a));
        system.Relationships.Add(Equation("m==b", m, b));

        var flat = SystemFlattener.Flatten(system);

        flat.DegreesOfFreedom.Should().Be(0);
        flat.Determination.Should().Be(Determination.ExactlyDetermined);
        flat.UnknownsWithNoEquation.Select(u => u.Id).Should().Equal("orphan");
    }

    // ── Bindings ─────────────────────────────────────────────────────────────

    [Fact]
    public void ABoundVariableIsNotAnUnknown()
    {
        var m = Unbound("m");
        var a = Unbound("a");
        var product = new ProductExpression { Id = "p" };
        product.AddFactor(m);
        product.AddFactor(a);

        var system = ExpressionSystem.Create("bindings", "");
        system.DirectExpressions.Add(m);
        system.DirectExpressions.Add(a);
        system.DerivedExpressions.Add(product);

        var unpinned = SystemFlattener.Flatten(system);
        unpinned.Unknowns.Should().HaveCount(2);

        var pinned = SystemFlattener.Flatten(
            system,
            new Dictionary<string, Measurand>
            {
                ["m"] = new Quantity(2, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelErr(0.01)),
            });

        pinned.Unknowns.Select(u => u.Id).Should().Equal("a");
    }

    [Fact]
    public void BindingsDoNotMutateTheModel()
    {
        var m = Unbound("m");
        var system = ExpressionSystem.Create("no mutation", "");
        system.DirectExpressions.Add(m);

        SystemFlattener.Flatten(
            system,
            new Dictionary<string, Measurand>
            {
                ["m"] = new Quantity(2, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelErr(0.01)),
            });

        // The whole point of passing bindings rather than assigning: a solver can probe a system at trial values
        // without leaving scratch values behind in the caller's model.
        m.Value.Should().BeNull();
        SystemFlattener.Flatten(system).Unknowns.Should().Equal(m);
    }

    [Fact]
    public void PinningDifferentSubsetsRepositionsWhichEquationsAreLeftOver()
    {
        // The over-determined interrogation the design calls for: pin one measurement, and the other becomes a
        // spare equation over a now-square system.
        var m = Unbound("m");
        var a = Bound("a", 5);
        var b = Bound("b", 5);

        var system = ExpressionSystem.Create("reconciliation shape", "");
        system.DirectExpressions.Add(m);
        system.Relationships.Add(Equation("m==a", m, a));
        system.Relationships.Add(Equation("m==b", m, b));

        SystemFlattener.Flatten(system).Determination.Should().Be(Determination.Overdetermined);

        var pinned = SystemFlattener.Flatten(
            system,
            new Dictionary<string, Measurand>
            {
                ["m"] = new Quantity(5, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelErr(0.01)),
            });

        pinned.Unknowns.Should().BeEmpty();
        pinned.Equations.Should().HaveCount(2);
        pinned.Equations.Should().OnlyContain(e => e.Unknowns.Count == 0);
        pinned.DegreesOfFreedom.Should().Be(-2);
    }

    private sealed class AlwaysEqual : IEqualityEstimating
    {
        public bool AreEqual(Measurand lhs, Measurand rhs) => true;
    }
}
