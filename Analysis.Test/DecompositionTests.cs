using Calcusystem.Analysis.Enums;
using Calcusystem.Analysis.Extensions;
using Calcusystem.Analysis.Outcomes;
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

public class DecompositionTests
{
    private static Variable Unset(string symbol) => new(symbol, Dimensionality.Mass, symbol);

    private static Variable Valued(string symbol, double kmsValue) =>
        new(symbol,
            new Quantity(kmsValue, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelative(0.01)),
            symbol);

    private static EqualityOperator Equation(string id, IExpression lhs, IExpression rhs) =>
        new(AgreementRule.Nominal, SolvingRole.Equation) { Id = id, Lhs = lhs, Rhs = rhs };

    private static IEnumerable<string> Ids(SystemBlock block) => block.Unknowns.Select(u => u.Id);

    private static IEnumerable<string> EquationIds(SystemBlock block) =>
        block.Equations.Select(e => e.Relationship.Id);

    // ── The case the count cannot see ────────────────────────────────────────

    /* The headline. Four unknowns and four equations, so the count says ExactlyDetermined and a solver gate
       waves it through — while `orphan` has no equation at all and `m` has two competing for it. The +1 and
       the -1 cancel. */
    [Fact]
    public void AnExactlyDeterminedSystemCanStillHoldAnUnreachableUnknown()
    {
        var orphan = Unset("orphan");
        var m = Unset("m");
        var x = Unset("x");
        var y = Unset("y");

        var system = ExpressionSystem.Create("hidden singularity", "");
        system.Add(orphan);
        system.Add(Equation("m==a", m, Valued("a", 5)));
        system.Add(Equation("m==b", m, Valued("b", 5)));
        system.Add(Equation("x==3", x, Valued("3kg", 3)));
        system.Add(Equation("y==x+1", y, new SumExpression([x, Valued("1kg", 1)]) { Id = "x+1" }));

        var flat = system.Flatten();
        flat.DegreesOfFreedom.Should().Be(0);
        flat.Determination.Should().Be(Determination.ExactlyDetermined);

        var decomposition = flat.Decompose();

        Ids(decomposition.Underdetermined).Should().Equal("orphan");
        EquationIds(decomposition.Underdetermined).Should().BeEmpty();

        Ids(decomposition.WellDetermined).Should().BeEquivalentTo("x", "y");
        EquationIds(decomposition.WellDetermined).Should().BeEquivalentTo("x==3", "y==x+1");

        Ids(decomposition.Overdetermined).Should().Equal("m");
        EquationIds(decomposition.Overdetermined).Should().BeEquivalentTo("m==a", "m==b");

        decomposition.IsWellPosed.Should().BeFalse("the two counts agree only because two failures cancel");
        decomposition.DeterminableUnknowns.Select(u => u.Id).Should().BeEquivalentTo("m", "x", "y");
    }

    /* A decomposition breaks the count down, so the parts must add back up to it. If these disagreed, one of
       the two would be wrong. */
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ThePartsLocalDegreesOfFreedomSumToTheWholeSystems(int competingEquations)
    {
        var m = Unset("m");
        var orphan = Unset("orphan");

        var system = ExpressionSystem.Create("summation", "");
        system.Add(m);
        system.Add(orphan);
        for (var i = 0; i < competingEquations; i++)
            system.Add(Equation($"m==spec{i}", m, Valued($"spec{i}", 5)));

        // A check over values that are already known: excluded from the count, and so from the matching too.
        system.Add(Equation("a==b", Valued("a", 5), Valued("b", 5)));

        var flat = system.Flatten();
        var decomposition = flat.Decompose();

        var summed = decomposition.Underdetermined.DegreesOfFreedom
                   + decomposition.WellDetermined.DegreesOfFreedom
                   + decomposition.Overdetermined.DegreesOfFreedom;

        summed.Should().Be(flat.DegreesOfFreedom);
    }

    /* Letting an unknown appear in two parts, or dropping one, would report on a different model than the one
       handed in. */
    [Fact]
    public void EveryUnknownAndEveryLiveEquationLandsInExactlyOnePart()
    {
        var orphan = Unset("orphan");
        var m = Unset("m");
        var x = Unset("x");

        var system = ExpressionSystem.Create("partition", "");
        system.Add(orphan);
        system.Add(Equation("m==a", m, Valued("a", 5)));
        system.Add(Equation("m==b", m, Valued("b", 5)));
        system.Add(Equation("x==3", x, Valued("3kg", 3)));

        var flat = system.Flatten();
        var decomposition = flat.Decompose();

        SystemBlock[] parts =
            [decomposition.Underdetermined, decomposition.WellDetermined, decomposition.Overdetermined];

        parts.SelectMany(Ids).Should().BeEquivalentTo(flat.Unknowns.Select(u => u.Id));
        parts.SelectMany(Ids).Should().OnlyHaveUniqueItems();

        var live = flat.Equations.Except(flat.RedundantEquations).Select(e => e.Relationship.Id);
        parts.SelectMany(EquationIds).Should().BeEquivalentTo(live);
        parts.SelectMany(EquationIds).Should().OnlyHaveUniqueItems();
    }

    // ── Matching, not counting ───────────────────────────────────────────────

    /* Why a maximum matching and not a greedy one. Taken in order, `a+b == 10` claims `a` first and leaves
       `a == 4` with nothing — which greedy would report as a system both under- and over-determined.
       Augmenting moves the first equation onto `b`. */
    [Fact]
    public void AnEquationGivesUpItsUnknownWhenAnotherHasNoAlternative()
    {
        var a = Unset("a");
        var b = Unset("b");

        var system = ExpressionSystem.Create("augmenting path", "");
        system.Add(Equation("a+b==10", new SumExpression([a, b]) { Id = "a+b" }, Valued("10kg", 10)));
        system.Add(Equation("a==4", a, Valued("4kg", 4)));

        var decomposition = system.Decompose();

        decomposition.IsWellPosed.Should().BeTrue();
        decomposition.Underdetermined.IsEmpty.Should().BeTrue();
        decomposition.Overdetermined.IsEmpty.Should().BeTrue();
        Ids(decomposition.WellDetermined).Should().BeEquivalentTo("a", "b");
    }

    /* An unknown no equation touches cannot be matched, so it is always underdetermined. The converse fails,
       which is the point: `y` has an equation and is still unsolvable, because that equation is spoken for. */
    [Fact]
    public void UnknownsWithNoEquationAreASubsetOfTheUnderdeterminedPart()
    {
        var orphan = Unset("orphan");
        var x = Unset("x");
        var y = Unset("y");

        var system = ExpressionSystem.Create("subset", "");
        system.Add(orphan);
        // One equation reaching both x and y: it can determine one of them, never both.
        system.Add(Equation("x+y==10", new SumExpression([x, y]) { Id = "x+y" }, Valued("10kg", 10)));

        var flat = system.Flatten();
        var decomposition = flat.Decompose();

        flat.UnknownsWithNoEquation.Select(u => u.Id).Should().Equal("orphan");
        Ids(decomposition.Underdetermined).Should().BeEquivalentTo("orphan", "x", "y");
        EquationIds(decomposition.Underdetermined).Should().Equal("x+y==10");
        decomposition.DeterminableUnknowns.Should().BeEmpty();
    }

    // ── The plain cases ──────────────────────────────────────────────────────

    /* A chain stays whole: `x` outright, `y` through it. The order they must be solved in is the fine
       decomposition's business, not this one's. */
    [Fact]
    public void AChainOfDeterminedUnknownsIsWellPosed()
    {
        var x = Unset("x");
        var y = Unset("y");

        var system = ExpressionSystem.Create("chain", "");
        system.Add(Equation("x==3", x, Valued("3kg", 3)));
        system.Add(Equation("y==x+1", y, new SumExpression([x, Valued("1kg", 1)]) { Id = "x+1" }));

        var decomposition = system.Decompose();

        decomposition.IsWellPosed.Should().BeTrue();
        Ids(decomposition.WellDetermined).Should().BeEquivalentTo("x", "y");
        decomposition.WellDetermined.Determination.Should().Be(Determination.ExactlyDetermined);
    }

    [Fact]
    public void ASystemWithNothingToSolveDecomposesIntoThreeEmptyParts()
    {
        var system = ExpressionSystem.Create("nothing outstanding", "");
        system.Add(Equation("a==b", Valued("a", 5), Valued("b", 5)));

        var decomposition = system.Decompose();

        decomposition.IsWellPosed.Should().BeTrue();
        decomposition.Underdetermined.IsEmpty.Should().BeTrue();
        decomposition.WellDetermined.IsEmpty.Should().BeTrue();
        decomposition.Overdetermined.IsEmpty.Should().BeTrue();
    }

    /* A tolerance or ordering relationship bounds a value rather than producing one, so it is not an equation
       and takes no part — the subject stays underdetermined however many constraints it carries. */
    [Fact]
    public void ConstraintsDoNotDetermineTheirSubject()
    {
        var m = Unset("m");
        var spec = Valued("spec", 5);

        var system = ExpressionSystem.Create("constrained only", "");
        system.Add(m);
        system.Add(new WithinBindingToleranceOperator { Id = "tol", Lhs = m, Rhs = spec });
        system.Add(new DefinitelyLessThanOperator { Id = "lt", Lhs = m, Rhs = spec });

        var decomposition = system.Decompose();

        Ids(decomposition.Underdetermined).Should().Equal("m");
        decomposition.Underdetermined.Equations.Should().BeEmpty();
        decomposition.DeterminableUnknowns.Should().BeEmpty();
    }

    // ── Overrides ────────────────────────────────────────────────────────────

    /* Decomposing goes through Flatten, so a pinned variable is not an unknown here either. Pinning the
       contested one turns the two competing equations into redundancy checks, which take no part at all. */
    [Fact]
    public void PinningAnUnknownRemovesItAndTheEquationsThatCompetedForIt()
    {
        var m = Unset("m");

        var system = ExpressionSystem.Create("pinned", "");
        system.Add(Equation("m==a", m, Valued("a", 5)));
        system.Add(Equation("m==b", m, Valued("b", 5)));

        system.Decompose().Overdetermined.IsEmpty.Should().BeFalse();

        var pinned = system.Decompose(
            new Dictionary<Variable, Measurand>
            {
                [m] = new Quantity(5, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelative(0.01)),
            });

        pinned.IsWellPosed.Should().BeTrue();
        pinned.Overdetermined.IsEmpty.Should().BeTrue();
        m.Value.Should().BeNull("decomposing must not write trial values into the model");
    }
}
