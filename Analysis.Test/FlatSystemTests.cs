using Calcusystem.Analysis.Enums;
using Calcusystem.Analysis.Extensions;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Dimensions;
using Calcusystem.Measurement.Quantities;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
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
        new(AgreementRule.Nominal, SolvingRole.Equation) { Id = id, Lhs = lhs, Rhs = rhs };

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
        system.Add(a);
        system.Add(c);
        system.Add(twoSeconds);
        system.Add(b);
        system.Add(Equation("b==c", b, c));
        system.Add(Equation("c==2s", c, twoSeconds));

        var flat = system.Flatten();

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
        system.Add(a);
        system.Add(c);
        system.Add(b);
        system.Add(Equation("b==c", b, c));

        var flat = system.Flatten();

        flat.Unknowns.Select(u => u.Id).Should().Equal("a");
        flat.Equations.Should().HaveCount(1);
        flat.DegreesOfFreedom.Should().Be(0);
    }

    [Fact]
    public void VariablesReachableOnlyThroughDerivedExpressionsAreStillUnknowns()
    {
        var m = Unbound("m");
        var product = new ProductExpression([m, Unbound("a")]) { Id = "p" };
        var system = ExpressionSystem.Create("derived only", "");
        system.Add(product);

        var flat = system.Flatten();

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
        system.Add(m);
        system.Add(negated);
        system.Add(Equation("eq", m, negated));

        var flat = system.Flatten();

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
        system.Add(m);
        system.Add(spec);
        system.Add(new WithinBindingToleranceOperator { Id = "tol", Lhs = m, Rhs = spec });
        system.Add(new DefinitelyLessThanOperator { Id = "lt", Lhs = m, Rhs = spec });
        system.Add(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Requirement)
        {
            Id = "check", Lhs = m, Rhs = spec
        });

        var flat = system.Flatten();

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
        system.Add(length);
        system.Add(limit);
        system.Add(new DefinitelyLessThanOperator { Id = "l<3m", Lhs = length, Rhs = limit });

        var flat = system.Flatten();

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
        system.Add(m);
        system.Add(Equation("m==a", m, a));
        system.Add(Equation("m==b", m, b));

        var flat = system.Flatten();

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
        system.Add(m);
        system.Add(orphan);
        system.Add(Equation("m==a", m, a));
        system.Add(Equation("m==b", m, b));

        var flat = system.Flatten();

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
        var product = new ProductExpression([m, a]) { Id = "p" };
        var system = ExpressionSystem.Create("bindings", "");
        system.Add(m);
        system.Add(a);
        system.Add(product);

        var unpinned = system.Flatten();
        unpinned.Unknowns.Should().HaveCount(2);

        var pinned = system.Flatten(
            new Dictionary<Variable, Measurand>
            {
                [m] = new Quantity(2, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelErr(0.01)),
            });

        pinned.Unknowns.Select(u => u.Id).Should().Equal("a");
    }

    [Fact]
    public void BindingsDoNotMutateTheModel()
    {
        var m = Unbound("m");
        var system = ExpressionSystem.Create("no mutation", "");
        system.Add(m);

        system.Flatten(
            new Dictionary<Variable, Measurand>
            {
                [m] = new Quantity(2, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelErr(0.01)),
            });

        // The whole point of passing bindings rather than assigning: a solver can probe a system at trial values
        // without leaving scratch values behind in the caller's model.
        m.Value.Should().BeNull();
        system.Flatten().Unknowns.Should().Equal(m);
    }

    [Fact]
    public void PinningDifferentSubsetsRepositionsWhichEquationsAreLeftOver()
    {
        // The over-determined interrogation the design calls for. Unpinned, two equations compete to determine
        // one unknown. Pinning that unknown leaves nothing to determine, so *both* equations become redundancy
        // checks — they still hold values to compare, which is the entry point for reconciliation, but neither
        // removes a degree of freedom from a system that has none left.
        var m = Unbound("m");
        var a = Bound("a", 5);
        var b = Bound("b", 5);

        var system = ExpressionSystem.Create("reconciliation shape", "");
        system.Add(m);
        system.Add(Equation("m==a", m, a));
        system.Add(Equation("m==b", m, b));

        system.Flatten().Determination.Should().Be(Determination.Overdetermined);

        var pinned = system.Flatten(
            new Dictionary<Variable, Measurand>
            {
                [m] = new Quantity(5, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelErr(0.01)),
            });

        pinned.Unknowns.Should().BeEmpty();
        pinned.Equations.Should().HaveCount(2, "both are still equations of the model");
        pinned.RedundantEquations.Select(e => e.Relationship.Id).Should().Equal("m==a", "m==b");
        pinned.DegreesOfFreedom.Should().Be(0, "neither equation can determine anything now");
        pinned.Determination.Should().Be(Determination.ExactlyDetermined);
    }

    // ── Vacuous equations ────────────────────────────────────────────────────

    /// <remarks>
    /// An equation over values that are all already known determines nothing, so it must not be subtracted.
    /// Counting it reported this system — one genuinely free variable, plus a redundancy check over two bound
    /// values — as square, which is the one classification that would let a solver gate wave it through.
    /// </remarks>
    [Fact]
    public void AnEquationWithNoIncidentUnknownsRemovesNoDegreeOfFreedom()
    {
        var x = Unbound("x");
        var a = Bound("a", 5);
        var b = Bound("b", 5);

        var system = ExpressionSystem.Create("redundant check beside a free variable", "");
        system.Add(x);
        system.Add(Equation("a==b", a, b));

        var flat = system.Flatten();

        flat.Unknowns.Should().Equal(x);
        flat.DegreesOfFreedom.Should().Be(1);
        flat.Determination.Should().Be(Determination.Underdetermined);
        flat.RedundantEquations.Select(e => e.Relationship.Id).Should().Equal("a==b");
    }

    /// <remarks>
    /// Why <c>Determination</c> reads the count alone rather than also weighing redundancy: the two are
    /// orthogonal. A vacuous equation touches no unknown, so the same redundancy check appended to an under-,
    /// exactly-, or over-determined system leaves each of them exactly as it was. Folding vacuity into the
    /// classification would report the middle case as over-determined, when its solve is square and the check
    /// concerns values that were already known.
    /// </remarks>
    [Theory]
    [InlineData(0, 1, Determination.Underdetermined)]
    [InlineData(1, 0, Determination.ExactlyDetermined)]
    [InlineData(2, -1, Determination.Overdetermined)]
    public void ARedundantCheckDoesNotChangeDeterminationWhateverTheSystem(
        int liveEquations, int expectedDoF, Determination expected)
    {
        var m = Unbound("m");
        var system = ExpressionSystem.Create("orthogonality", "");
        system.Add(m);

        // 0, 1, or 2 equations competing to determine the single unknown.
        for (var i = 0; i < liveEquations; i++)
            system.Add(Equation($"m==spec{i}", m, Bound($"spec{i}", 5)));

        var withoutCheck = system.Flatten();

        // The same redundancy check, over values that were already known, appended to each.
        system.Add(Equation("a==b", Bound("a", 5), Bound("b", 5)));
        var withCheck = system.Flatten();

        withoutCheck.DegreesOfFreedom.Should().Be(expectedDoF);
        withCheck.DegreesOfFreedom.Should().Be(expectedDoF, "a vacuous equation determines nothing");
        withCheck.Determination.Should().Be(expected);
        withCheck.RedundantEquations.Select(e => e.Relationship.Id).Should().Equal("a==b");
    }
}
