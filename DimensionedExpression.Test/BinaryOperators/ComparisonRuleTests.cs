using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.BinaryOperators;

/// <summary>
/// The atom every operator is now built from: one landmark of the subject against one landmark of the criterion,
/// at a stated strictness. These pin the three things the design leans on — that the notation is systematic
/// enough to be generated, that mirroring is really the swap it claims to be, and that "no answer" survives the
/// conjunction instead of collapsing into "no".
/// </summary>
public class ComparisonRuleTests
{
    private static Measurand M(double value, double lowerError, double upperError) =>
        Mass.Kilogram.Quantity(value).Measurand(
            AsymmetricUncertainty.FromAbsolute(
                Mass.Kilogram.Quantity(upperError), Mass.Kilogram.Quantity(lowerError)));

    private static Measurand Metres(double value) =>
        Length.Meter.Quantity(value).Measurand(SymmetricUncertainty.FromRelative(0));

    private static readonly Landmark[] Landmarks =
        [Landmark.LowerBound, Landmark.Nominal, Landmark.UpperBound];

    private static readonly MustBe[] Masks =
    [
        MustBe.LessThan, MustBe.EqualTo, MustBe.GreaterThan,
        MustBe.LessThanOrEqualTo, MustBe.GreaterThanOrEqualTo, MustBe.InequalTo,
        MustBe.Comparable,
    ];

    private static IEnumerable<ComparisonRule> AllRules() =>
        from lhs in Landmarks
        from mask in Masks
        from rhs in Landmarks
        select new ComparisonRule(lhs, mask, rhs);

    private static IEnumerable<Measurand> Grid()
    {
        double[] values = [9, 10, 11];
        double[] errors = [0, 0.5, 1];

        return from v in values from lower in errors from upper in errors select M(v, lower, upper);
    }

    private static IEnumerable<(Measurand Lhs, Measurand Rhs)> Pairs() =>
        from lhs in Grid() from rhs in Grid() select (lhs, rhs);

    // ── The notation is systematic ────────────────────────────────────────────

    /// <summary>Every operator asserting exactly one rule, paired with the symbol it declares by hand.</summary>
    private static IEnumerable<BinaryOperatorBase> SingleRuleOperators()
    {
        var x = new Variable("x", Mass.Kilogram.Dimensionality);

        return new BinaryOperatorBase[]
        {
            new DefinitelyLessThanOperator { Id = "a", Lhs = x, Rhs = x },
            new UpperBoundsLessThanOperator { Id = "b", Lhs = x, Rhs = x },
            new NominallyLessThanOperator { Id = "c", Lhs = x, Rhs = x },
            new DefinitelyGreaterThanOperator { Id = "d", Lhs = x, Rhs = x },
            new LowerBoundsGreaterThanOperator { Id = "e", Lhs = x, Rhs = x },
            new NominallyGreaterThanOperator { Id = "f", Lhs = x, Rhs = x },
        };
    }

    /// <remarks>
    /// The claim the whole glyph alphabet rests on: the bar picks the statistic, the corner picks the side, and
    /// those two choices <i>determine</i> the symbol. If they do, generating it is safe; if they do not, the
    /// notation was decoration and the six hand-written symbols were the real spelling. Six operators declare
    /// theirs independently, and generation has to land on all six.
    /// </remarks>
    [Fact]
    public void TheGeneratedSymbolReproducesEveryHandWrittenOrderingSymbol()
    {
        foreach (var op in SingleRuleOperators())
        {
            op.Rules.Should().ContainSingle("{0} asserts one rule", op.Symbol);
            op.Rules.Single().Symbol.Should().Be(op.Symbol);
        }
    }

    /// <remarks>
    /// Distinct rules must not collide, or the notation would be ambiguous rather than merely terse — nine
    /// landmark pairs times seven masks, all separate.
    /// </remarks>
    [Fact]
    public void DistinctRulesGetDistinctSymbols()
    {
        var rules = AllRules().ToList();

        rules.Select(r => r.Symbol).Should().OnlyHaveUniqueItems();
        rules.Should().HaveCount(63);
    }

    // ── Mirroring ─────────────────────────────────────────────────────────────

    /// <remarks>
    /// The property that lets each mirrored pair of operators be declared once. Asserted as a differential over
    /// the grid rather than by inspecting the triple, because what matters is the <i>verdict</i> agreeing, and
    /// that also puts <c>MeasurandComparer</c>'s antisymmetry under test.
    /// </remarks>
    [Fact]
    public void MirroringARuleIsTheSameAsSwappingTheOperands()
    {
        foreach (var rule in AllRules())
        {
            foreach (var (lhs, rhs) in Pairs())
            {
                rule.Mirrored.IsSatisfiedGiven(lhs, rhs)
                    .Should().Be(rule.IsSatisfiedGiven(rhs, lhs), $"{rule.Symbol} over {lhs} / {rhs}");
            }
        }
    }

    [Fact]
    public void MirroringTwiceGivesTheRuleBack() =>
        AllRules().Should().OnlyContain(r => r.Mirrored.Mirrored == r);

    /// <remarks>
    /// Mirroring is what lets <see cref="OrderingLadder.RuleFor"/> define one direction and derive the other, so
    /// the mirror of a tier has to be that tier's counterpart and not merely something similar. The operators no
    /// longer rely on this — they state their own rules — but the ladder still does.
    /// </remarks>
    [Theory]
    [InlineData(OrderingConfidence.Certain, "⌜<⌟", "⌞>⌝")]
    [InlineData(OrderingConfidence.Nominal, "·<·", "·>·")]
    [InlineData(OrderingConfidence.Possible, "⌞<⌝", "⌜>⌟")]
    public void EachOrderingTierMirrorsToItsCounterpartInTheOtherDirection(
        OrderingConfidence tier, string below, string above)
    {
        var descending = OrderingLadder.RuleFor(OrderingDirection.Below, tier);

        descending.Symbol.Should().Be(below);
        descending.Mirrored.Symbol.Should().Be(above);
        OrderingLadder.RuleFor(OrderingDirection.Above, tier).Should().Be(descending.Mirrored);
    }

    // ── Three-valued conjunction ──────────────────────────────────────────────

    /// <remarks>
    /// The reason the seam had to become <c>bool?</c>. A comparison with no answer must not read as a violation:
    /// a report that turned "these carry different dimensions" into "this requirement failed" would send an
    /// engineer looking for a problem in the wrong place entirely.
    /// </remarks>
    [Fact]
    public void AnUnanswerableComparisonIsNullRatherThanFalse()
    {
        var rule = new ComparisonRule(Landmark.Nominal, MustBe.LessThan, Landmark.Nominal);

        rule.IsSatisfiedGiven(M(1, 0, 0), Metres(2)).Should().BeNull();
    }

    /// <summary>
    /// Two unbounded uncertainties: both ceilings are +∞, which is the one shape that makes a ceiling-against-
    /// ceiling comparison unanswerable while the reported values stay perfectly comparable.
    /// </summary>
    private static readonly ComparisonRule CeilingBelowCeiling =
        new(Landmark.UpperBound, MustBe.LessThan, Landmark.UpperBound);

    private static readonly ComparisonRule NominalBelowNominal =
        new(Landmark.Nominal, MustBe.LessThan, Landmark.Nominal);

    [Fact]
    public void ADefiniteFailureBeatsAnUnanswerableRule()
    {
        // Ceilings: +∞ against +∞, no answer. Reported values: 10 against 5, definitely violated. False wins.
        ComparisonRule.AllSatisfied(
            [CeilingBelowCeiling, NominalBelowNominal],
            M(10, 0, double.PositiveInfinity),
            M(5, 0, double.PositiveInfinity))
        .Should().BeFalse();
    }

    [Fact]
    public void AnUnanswerableRuleLeavesAnOtherwiseSatisfiedConjunctionUnknown()
    {
        ComparisonRule.AllSatisfied(
            [CeilingBelowCeiling, NominalBelowNominal],
            M(5, 0, double.PositiveInfinity),
            M(10, 0, double.PositiveInfinity))
        .Should().BeNull();
    }

    /// <remarks>
    /// Why nullability is per rung and not per evaluation, and why it had to survive the conjunction. Both sides
    /// have unbounded uncertainty upward, so no comparison of their ceilings has an answer — but where the
    /// reported value sits in the band is still perfectly decidable, and that rung answers.
    /// </remarks>
    [Fact]
    public void OneLadderRungCanBeUnknownWhileTheOthersAnswer()
    {
        // Subject reported at 15 with no ceiling; band from 10 with no ceiling either.
        var subject = M(15, 0, double.PositiveInfinity);
        var band = M(10, 0, double.PositiveInfinity);

        ContainmentLadder.Reaches(subject, band, ContainmentRung.NominalWithin)
            .Should().BeTrue("15 is above the band's floor and below its unbounded ceiling");
        ContainmentLadder.Reaches(subject, band, ContainmentRung.NominalAndUpperWithin)
            .Should().BeNull("both ceilings are +∞, so neither is below the other");
    }

    // ── Operators and ladder rungs are one declaration ────────────────────────

    // ── SimpleComparison ──────────────────────────────────────────────────────

    /// <remarks>
    /// The spelling that motivated the general form: a conservative acceptance criterion comparing the reported
    /// value against the criterion's guaranteed floor. No named operator says this, and it needs no class.
    /// </remarks>
    [Fact]
    public void SimpleComparisonSpellsARelationNoNamedOperatorHas()
    {
        var x = new Variable("x", Mass.Kilogram.Dimensionality);
        var op = new SimpleComparison(
            new ComparisonRule(Landmark.Nominal, MustBe.LessThan, Landmark.LowerBound))
        {
            Id = "conservative", Lhs = x, Rhs = x,
        };

        op.Symbol.Should().Be("·<⌟");

        // Reported 9 against a band whose floor is 9.5 — below the guarantee, so it holds.
        op.IsSatisfiedGiven(M(9, 1, 1), M(10, 0.5, 0.5)).Should().BeTrue();

        // Reported 9.8 is inside the band, so it does not.
        op.IsSatisfiedGiven(M(9.8, 1, 1), M(10, 0.5, 0.5)).Should().BeFalse();
    }

    /// <remarks>
    /// A rule accepting no outcome is never satisfied, so it would report as a violation of something the model
    /// never asserted. It is also the mask enum's zero, which makes it what a forgotten field reads as — so the
    /// refusal turns a silent phantom finding into an error where the mistake was made.
    /// </remarks>
    [Fact]
    public void ASimpleComparisonThatCanNeverBeSatisfiedIsRefused()
    {
        var x = new Variable("x", Mass.Kilogram.Dimensionality);

        // The refusal happens in the constructor, before the required members are ever assigned.
        var act = () => new SimpleComparison(
            new ComparisonRule(Landmark.Nominal, MustBe.Impossible, Landmark.Nominal))
        {
            Id = "never", Lhs = x, Rhs = x,
        };

        act.Should().Throw<ArgumentException>().WithMessage("*never be satisfied*");
    }

    /// <remarks>
    /// <c>Any</c> looks like the same mistake and is not. Under a three-valued seam it is not a tautology: it
    /// answers true when the landmarks can be compared and null when they cannot, so it is the only way to
    /// spell "both of these are well-defined quantities". Refusing it would remove a check nothing else offers.
    /// </remarks>
    [Fact]
    public void AnAcceptAnythingComparisonIsAComparabilityCheckAndIsAllowed()
    {
        var x = new Variable("x", Mass.Kilogram.Dimensionality);
        var op = new SimpleComparison(
            new ComparisonRule(Landmark.UpperBound, MustBe.Comparable, Landmark.UpperBound))
        {
            Id = "well-defined", Lhs = x, Rhs = x,
        };

        op.Symbol.Should().Be("⌜?⌝");
        op.IsSatisfiedGiven(M(1, 0.1, 0.1), M(99, 0.1, 0.1))
            .Should().BeTrue("both ceilings are ordinary numbers");
        op.IsSatisfiedGiven(M(1, 0, double.PositiveInfinity), M(9, 0, double.PositiveInfinity))
            .Should().BeNull("neither ceiling is a quantity at all");
    }

    [Fact]
    public void SimpleComparisonIsAlwaysARequirement()
    {
        var x = new Variable("x", Mass.Kilogram.Dimensionality);
        var op = new SimpleComparison(
            new ComparisonRule(Landmark.Nominal, MustBe.LessThan, Landmark.LowerBound))
        {
            Id = "r", Lhs = x, Rhs = x,
        };

        op.SolvingRole.Should().Be(SolvingRole.Requirement);
        op.IsDetermining.Should().BeFalse();
    }
}
