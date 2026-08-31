using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Enums;
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
            AsymmetricUncertainty.FromAbsErr(
                Mass.Kilogram.Quantity(upperError), Mass.Kilogram.Quantity(lowerError)));

    private static Measurand Metres(double value) =>
        Length.Meter.Quantity(value).Measurand(SymmetricUncertainty.FromRelErr(0));

    private static readonly Landmark[] Landmarks =
        [Landmark.LowerBound, Landmark.Nominal, Landmark.UpperBound];

    private static readonly ComparisonType[] Masks =
    [
        ComparisonType.LessThan, ComparisonType.EqualTo, ComparisonType.GreaterThan,
        ComparisonType.LessThanOrEqualTo, ComparisonType.GreaterThanOrEqualTo, ComparisonType.InequalTo,
        ComparisonType.Any,
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
    /// The pairing the operator classes actually rely on: the greater-than family is declared as the less-than
    /// family mirrored, so the mirror of a tier has to be the tier's counterpart and not merely something
    /// similar.
    /// </remarks>
    [Fact]
    public void MirroringTheOrderingTiersGivesTheGreaterThanFamily()
    {
        OrderingLadder.Certainly.Mirrored.Symbol.Should().Be("⌞>⌝");
        OrderingLadder.Nominally.Mirrored.Symbol.Should().Be("·>·");
        OrderingLadder.Possibly.Mirrored.Symbol.Should().Be("⌜>⌟");
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
        var rule = new ComparisonRule(Landmark.Nominal, ComparisonType.LessThan, Landmark.Nominal);

        rule.IsSatisfiedGiven(M(1, 0, 0), Metres(2)).Should().BeNull();
    }

    /// <summary>
    /// Two unbounded uncertainties: both ceilings are +∞, which is the one shape that makes a ceiling-against-
    /// ceiling comparison unanswerable while the reported values stay perfectly comparable.
    /// </summary>
    private static readonly ComparisonRule CeilingBelowCeiling =
        new(Landmark.UpperBound, ComparisonType.LessThan, Landmark.UpperBound);

    private static readonly ComparisonRule NominalBelowNominal =
        new(Landmark.Nominal, ComparisonType.LessThan, Landmark.Nominal);

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
        var ladder = ContainmentLadder.Evaluate(
            M(15, 0, double.PositiveInfinity),
            M(10, 0, double.PositiveInfinity));

        ladder.NominalWithin.Should().BeTrue("15 is above the band's floor and below its unbounded ceiling");
        ladder.NominalAndUpperWithin.Should().BeNull("both ceilings are +∞, so neither is below the other");
    }

    // ── Operators and ladder rungs are one declaration ────────────────────────

    /// <remarks>
    /// The duplication this design exists to prevent. A rung and the operator named after it must be the same
    /// triples, not two descriptions that happen to agree today — that is exactly the drift the previous
    /// refactor removed and this one could have reintroduced.
    /// </remarks>
    [Theory]
    [MemberData(nameof(RungOperatorPairs))]
    public void EachNamedOperatorAssertsExactlyItsLadderRung(
        IReadOnlyList<ComparisonRule> rung, IBinaryOperator op) =>
        ((BinaryOperatorBase)op).Rules.Should().Equal(rung);

    public static TheoryData<IReadOnlyList<ComparisonRule>, IBinaryOperator> RungOperatorPairs()
    {
        var x = new Variable("x", Mass.Kilogram.Dimensionality);

        return new TheoryData<IReadOnlyList<ComparisonRule>, IBinaryOperator>
        {
            { [OrderingLadder.Certainly], new DefinitelyLessThanOperator { Id = "a", Lhs = x, Rhs = x } },
            { [OrderingLadder.Nominally], new NominallyLessThanOperator { Id = "b", Lhs = x, Rhs = x } },
            {
                [OrderingLadder.Certainly.Mirrored],
                new DefinitelyGreaterThanOperator { Id = "c", Lhs = x, Rhs = x }
            },
            {
                [OrderingLadder.Nominally.Mirrored],
                new NominallyGreaterThanOperator { Id = "d", Lhs = x, Rhs = x }
            },
            {
                ContainmentLadder.OverlapsRules,
                new AnyToleranceOverlapOperator { Id = "e", Lhs = x, Rhs = x }
            },
            {
                ContainmentLadder.NominalWithinRules,
                new WithinBindingToleranceOperator { Id = "f", Lhs = x, Rhs = x }
            },
            {
                ContainmentLadder.NominalAndUpperWithinRules,
                new PointAndUpperBoundWithinToleranceOperator { Id = "g", Lhs = x, Rhs = x }
            },
            {
                ContainmentLadder.NominalAndLowerWithinRules,
                new PointAndLowerBoundWithinToleranceOperator { Id = "h", Lhs = x, Rhs = x }
            },
            {
                ContainmentLadder.WhollyWithinRules,
                new WhollyWithinToleranceOperator { Id = "i", Lhs = x, Rhs = x }
            },
        };
    }

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
            new ComparisonRule(Landmark.Nominal, ComparisonType.LessThan, Landmark.LowerBound))
        {
            Id = "conservative", Lhs = x, Rhs = x,
        };

        op.Symbol.Should().Be("·<⌟");

        // Reported 9 against a band whose floor is 9.5 — below the guarantee, so it holds.
        op.IsSatisfiedGiven(M(9, 1, 1), M(10, 0.5, 0.5)).Should().BeTrue();

        // Reported 9.8 is inside the band, so it does not.
        op.IsSatisfiedGiven(M(9.8, 1, 1), M(10, 0.5, 0.5)).Should().BeFalse();
    }

    [Fact]
    public void SimpleComparisonIsAlwaysARequirement()
    {
        var x = new Variable("x", Mass.Kilogram.Dimensionality);
        var op = new SimpleComparison(
            new ComparisonRule(Landmark.Nominal, ComparisonType.LessThan, Landmark.LowerBound))
        {
            Id = "r", Lhs = x, Rhs = x,
        };

        op.SolvingRole.Should().Be(SolvingRole.Requirement);
        op.IsDetermining.Should().BeFalse();
    }
}
