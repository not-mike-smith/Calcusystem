using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.BinaryOperators;

/// <summary>
/// The ladders beside the named operators: under uncertainty a comparison has several nested answers. These pin
/// the nesting, the one place it deliberately stops, that an operator's own rule is discoverable as the rung it
/// claims to be, and — most importantly — that none of this changed an answer anywhere.
/// </summary>
public class ConfidenceLadderTests
{
    /// <summary>A measurand at <paramref name="value"/> kg with the given absolute error bars.</summary>
    private static Measurand M(double value, double lowerError, double upperError) =>
        Mass.Kilogram.Quantity(value).Measurand(
            AsymmetricUncertainty.FromAbsErr(
                Mass.Kilogram.Quantity(upperError), Mass.Kilogram.Quantity(lowerError)));

    /// <summary>
    /// Values and error bars chosen so bounds coincide exactly and often — identical intervals, intervals that
    /// merely touch, zero-width intervals. Those are where a strict and a non-strict comparison part company,
    /// so a sweep of "generic" inputs would miss precisely the cases worth checking.
    /// </summary>
    private static IEnumerable<Measurand> Grid()
    {
        double[] values = [9, 9.5, 10, 10.5, 11];
        double[] errors = [0, 0.5, 1];

        return from v in values
               from lower in errors
               from upper in errors
               select M(v, lower, upper);
    }

    private static IEnumerable<(Measurand Lhs, Measurand Rhs)> Pairs() =>
        from lhs in Grid()
        from rhs in Grid()
        select (lhs, rhs);

    // ── Behaviour preservation ────────────────────────────────────────────────

    /// <summary>
    /// The oracle: every operator's condition exactly as it was written before the ladder existed, transcribed
    /// from the pre-refactor bodies.
    /// </summary>
    private static readonly (string Symbol, Func<Measurand, Measurand, bool> Original)[] _originalConditions =
    [
        ("<<", (a, b) => a.KmsValue + a.KmsUpperAbsoluteError < b.KmsValue - b.KmsLowerAbsoluteError),
        ("<^", (a, b) => a.KmsValue + a.KmsUpperAbsoluteError < b.KmsValue + b.KmsUpperAbsoluteError),
        ("<~", (a, b) => a.KmsValue < b.KmsValue),
        (">>", (a, b) => a.KmsValue - a.KmsLowerAbsoluteError > b.KmsValue + b.KmsUpperAbsoluteError),
        (">v", (a, b) => a.KmsValue - a.KmsLowerAbsoluteError > b.KmsValue - b.KmsLowerAbsoluteError),
        (">~", (a, b) => a.KmsValue > b.KmsValue),
        ("=}", (a, b) => a.KmsValue >= b.KmsValue - b.KmsLowerAbsoluteError &&
                         a.KmsValue <= b.KmsValue + b.KmsUpperAbsoluteError),
        ("⌈=}", (a, b) => a.KmsValue >= b.KmsValue - b.KmsLowerAbsoluteError &&
                          a.KmsValue + a.KmsUpperAbsoluteError <= b.KmsValue + b.KmsUpperAbsoluteError),
        ("⌊=}", (a, b) => a.KmsValue <= b.KmsValue + b.KmsUpperAbsoluteError &&
                          a.KmsValue - a.KmsLowerAbsoluteError >= b.KmsValue - b.KmsLowerAbsoluteError),
        ("[=}", (a, b) => a.KmsValue - a.KmsLowerAbsoluteError > b.KmsValue - b.KmsLowerAbsoluteError &&
                          a.KmsValue + a.KmsUpperAbsoluteError < b.KmsValue + b.KmsUpperAbsoluteError),
        ("≈", OriginalOverlap),
        ("≃", (a, b) => OriginalMutual(a, b) && OriginalMutual(b, a)),
    ];

    private static bool OriginalOverlap(Measurand lhs, Measurand rhs)
    {
        var (smaller, bigger) = lhs.KmsValue < rhs.KmsValue ? (lhs, rhs) : (rhs, lhs);
        return smaller.KmsValue + smaller.KmsUpperAbsoluteError >=
               bigger.KmsValue - bigger.KmsLowerAbsoluteError;
    }

    private static bool OriginalMutual(Measurand x, Measurand y) =>
        x.KmsValue >= y.KmsValue - y.KmsLowerAbsoluteError &&
        x.KmsValue <= y.KmsValue + y.KmsUpperAbsoluteError;

    private static IBinaryOperator OperatorFor(string symbol, IExpression lhs, IExpression rhs) => symbol switch
    {
        "<<" => new DefinitelyLessThanOperator { Id = "o", Lhs = lhs, Rhs = rhs },
        "<^" => new UpperBoundsLessThanOperator { Id = "o", Lhs = lhs, Rhs = rhs },
        "<~" => new NominallyLessThanOperator { Id = "o", Lhs = lhs, Rhs = rhs },
        ">>" => new DefinitelyGreaterThanOperator { Id = "o", Lhs = lhs, Rhs = rhs },
        ">v" => new LowerBoundsGreaterThanOperator { Id = "o", Lhs = lhs, Rhs = rhs },
        ">~" => new NominallyGreaterThanOperator { Id = "o", Lhs = lhs, Rhs = rhs },
        "=}" => new WithinBindingToleranceOperator { Id = "o", Lhs = lhs, Rhs = rhs },
        "⌈=}" => new PointAndUpperBoundWithinToleranceOperator { Id = "o", Lhs = lhs, Rhs = rhs },
        "⌊=}" => new PointAndLowerBoundWithinToleranceOperator { Id = "o", Lhs = lhs, Rhs = rhs },
        "[=}" => new WhollyWithinToleranceOperator { Id = "o", Lhs = lhs, Rhs = rhs },
        "≈" => new AnyToleranceOverlapOperator { Id = "o", Lhs = lhs, Rhs = rhs },
        "≃" => new MutuallyWithinToleranceOperator { Id = "o", Lhs = lhs, Rhs = rhs },
        _ => throw new ArgumentException(symbol),
    };

    /// <remarks>
    /// <para>
    /// The real proof the ladder changed nothing. The pre-existing suite is 65 hand-picked cases; this is every
    /// operator against its original formula over 2,025 pairs built to land on exact boundary coincidences,
    /// which is where a rewrite of interval comparisons would actually go wrong.
    /// </para>
    /// <para>
    /// It has since survived a second rewrite, onto declared <c>ComparisonRule</c>s evaluated by
    /// <c>MeasurandComparer</c>, and that was not a foregone conclusion — comparison became tolerance-aware,
    /// which is a genuine behaviour change. It does not show up here because the grid's values are separated by
    /// far more than any measurement resolves. Where it does show up is pinned separately, in
    /// <c>UnboundedUncertaintyTests</c>. Keep both: this one guards everything the change was <i>not</i> meant
    /// to touch, which is almost all of it.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("<<")]
    [InlineData("<^")]
    [InlineData("<~")]
    [InlineData(">>")]
    [InlineData(">v")]
    [InlineData(">~")]
    [InlineData("=}")]
    [InlineData("⌈=}")]
    [InlineData("⌊=}")]
    [InlineData("[=}")]
    [InlineData("≈")]
    [InlineData("≃")]
    public void EveryOperatorAgreesWithItsPreLadderFormulaAcrossTheGrid(string symbol)
    {
        var original = _originalConditions.Single(c => c.Symbol == symbol).Original;
        var placeholder = new Variable("x", Mass.Kilogram.Dimensionality);
        var op = OperatorFor(symbol, placeholder, placeholder);

        var divergences = Pairs()
            .Where(p => op.IsSatisfiedGiven(p.Lhs, p.Rhs) != original(p.Lhs, p.Rhs))
            .Select(p => $"{Describe(p.Lhs)} {symbol} {Describe(p.Rhs)}")
            .ToList();

        divergences.Should().BeEmpty();
    }

    private static string Describe(Measurand m) =>
        $"[{m.KmsValue - m.KmsLowerAbsoluteError}, {m.KmsValue + m.KmsUpperAbsoluteError}]";

    // ── Ordering: a clean chain ───────────────────────────────────────────────

    /// <remarks>
    /// Asserted in both directions now that direction is named rather than assumed. The chain has to hold for
    /// <c>Above</c> on its own terms — it is not enough that it holds for <c>Below</c> and that the rules happen
    /// to mirror.
    /// </remarks>
    [Theory]
    [InlineData(OrderingDirection.Below)]
    [InlineData(OrderingDirection.Above)]
    public void EachOrderingTierImpliesTheOneBelowIt(OrderingDirection direction)
    {
        foreach (var (lhs, rhs) in Pairs())
        {
            var because = $"{Describe(lhs)}/{Describe(rhs)} going {direction}";

            bool? Reaches(OrderingConfidence tier) =>
                OrderingLadder.Reaches(lhs, rhs, new OrderingRung(direction, tier));

            if (Reaches(OrderingConfidence.Certain) is true)
            {
                Reaches(OrderingConfidence.Nominal).Should().BeTrue("certain implies nominal for " + because);
            }

            if (Reaches(OrderingConfidence.Nominal) is true)
            {
                Reaches(OrderingConfidence.Possible).Should().BeTrue("nominal implies possible for " + because);
            }
        }
    }

    /// <remarks>
    /// The discovery half. An operator declares its comparison plainly and the ladder places it afterwards, so
    /// this is what now stops a rung and the operator named after it drifting apart — a stronger check than the
    /// old one, which only asserted the operator had been handed the ladder's own constant.
    /// </remarks>
    [Theory]
    [MemberData(nameof(LadderOperators))]
    public void EachOrderingOperatorsRuleIsDiscoverableAsItsRung(IBinaryOperator op, OrderingRung expected)
    {
        var rule = ((BinaryOperatorBase)op).Rules.Single();

        rule.RungOf().Should().Be(expected);
        rule.Should().Be(OrderingLadder.RuleFor(expected.Direction, expected.Confidence));
    }

    public static TheoryData<IBinaryOperator, OrderingRung> LadderOperators()
    {
        var x = new Variable("x", Mass.Kilogram.Dimensionality);

        return new TheoryData<IBinaryOperator, OrderingRung>
        {
            {
                new DefinitelyLessThanOperator { Id = "a", Lhs = x, Rhs = x },
                new OrderingRung(OrderingDirection.Below, OrderingConfidence.Certain)
            },
            {
                new NominallyLessThanOperator { Id = "b", Lhs = x, Rhs = x },
                new OrderingRung(OrderingDirection.Below, OrderingConfidence.Nominal)
            },
            {
                new DefinitelyGreaterThanOperator { Id = "c", Lhs = x, Rhs = x },
                new OrderingRung(OrderingDirection.Above, OrderingConfidence.Certain)
            },
            {
                new NominallyGreaterThanOperator { Id = "d", Lhs = x, Rhs = x },
                new OrderingRung(OrderingDirection.Above, OrderingConfidence.Nominal)
            },
        };
    }

    /// <remarks>
    /// The two statistic comparisons are off the ladder, and discovery is where that stops being a claim in a
    /// doc comment and becomes something the code answers.
    /// </remarks>
    [Fact]
    public void TheStatisticComparisonsAreNotRungsOfTheOrderingLadder()
    {
        var x = new Variable("x", Mass.Kilogram.Dimensionality);

        ((BinaryOperatorBase)new UpperBoundsLessThanOperator { Id = "a", Lhs = x, Rhs = x })
            .Rules.Single().RungOf().Should().BeNull();
        ((BinaryOperatorBase)new LowerBoundsGreaterThanOperator { Id = "b", Lhs = x, Rhs = x })
            .Rules.Single().RungOf().Should().BeNull();
    }

    /// <remarks>
    /// <c>Contradicted</c> is what is left when the weakest rung fails, so no rule tests it. Asking for one is a
    /// caller error rather than a lookup that quietly returns something plausible.
    /// </remarks>
    [Fact]
    public void ContradictedIsAResultAndNotARung()
    {
        var act = () => OrderingLadder.RuleFor(OrderingDirection.Below, OrderingConfidence.Contradicted);

        act.Should().Throw<ArgumentOutOfRangeException>();
        OrderingLadder.RuleFor(OrderingDirection.Below, OrderingConfidence.Certain)
            .RungOf()!.Value.Confidence.Should().NotBe(OrderingConfidence.Contradicted);
    }

    [Theory]
    // disjoint, lhs below: every tier
    [InlineData(9, 0, 0.1, 11, 0.1, 0, OrderingConfidence.Certain)]
    // overlapping but nominally ordered
    [InlineData(10, 0, 1, 10.5, 1, 0, OrderingConfidence.Nominal)]
    // nominally the wrong way round, but the intervals still permit it
    [InlineData(10.5, 1, 1, 10, 1, 1, OrderingConfidence.Possible)]
    // disjoint, lhs above: nothing holds
    [InlineData(11, 0.1, 0, 9, 0, 0.1, OrderingConfidence.Contradicted)]
    public void AchievedReportsTheStrongestTierReached(
        double lhsValue, double lhsLower, double lhsUpper,
        double rhsValue, double rhsLower, double rhsUpper,
        OrderingConfidence expected)
    {
        OrderingLadder.AchievedTier(
                M(lhsValue, lhsLower, lhsUpper), M(rhsValue, rhsLower, rhsUpper), OrderingDirection.Below)
            .Should().Be(expected);
    }

    /// <remarks>
    /// Reversing the operands reverses the direction, which is the mirroring property stated where a reader of
    /// the ladder can see it — rather than left implicit in how four operators were declared.
    /// </remarks>
    [Theory]
    [InlineData(9, 0, 0.1, 11, 0.1, 0, OrderingConfidence.Certain)]
    [InlineData(10, 0, 1, 10.5, 1, 0, OrderingConfidence.Nominal)]
    [InlineData(10.5, 1, 1, 10, 1, 1, OrderingConfidence.Possible)]
    [InlineData(11, 0.1, 0, 9, 0, 0.1, OrderingConfidence.Contradicted)]
    public void GoingAboveIsGoingBelowWithTheOperandsSwapped(
        double lhsValue, double lhsLower, double lhsUpper,
        double rhsValue, double rhsLower, double rhsUpper,
        OrderingConfidence expected)
    {
        var lhs = M(lhsValue, lhsLower, lhsUpper);
        var rhs = M(rhsValue, rhsLower, rhsUpper);

        OrderingLadder.AchievedTier(rhs, lhs, OrderingDirection.Above).Should().Be(expected);
    }

    [Fact]
    public void ReachesAsksForOneRungAndNotTheWholeLadder()
    {
        // 10 ± 1 against 10.5 ± 1: nominally ordered, intervals overlap.
        var lhs = M(10, 1, 1);
        var rhs = M(10.5, 1, 1);

        bool? Reaches(OrderingConfidence tier) =>
            OrderingLadder.Reaches(lhs, rhs, new OrderingRung(OrderingDirection.Below, tier));

        Reaches(OrderingConfidence.Contradicted).Should().BeTrue("every pair reaches the floor");
        Reaches(OrderingConfidence.Possible).Should().BeTrue();
        Reaches(OrderingConfidence.Nominal).Should().BeTrue();
        Reaches(OrderingConfidence.Certain).Should().BeFalse();
    }

    /// <remarks>
    /// The tier no named operator ever asked for, and the reason the ladder is worth keeping at all: a modeller
    /// who writes <c>·&lt;·</c> and gets <c>false</c> cannot otherwise tell "comfortably the other way round"
    /// from "a hair's breadth away, and the uncertainty covers it".
    /// </remarks>
    [Fact]
    public void PossibleIsReachableWithoutNominal()
    {
        // 10.5 ± 1 vs 10 ± 1 — nominally greater, but the intervals overlap heavily.
        var lhs = M(10.5, 1, 1);
        var rhs = M(10, 1, 1);

        bool? Reaches(OrderingConfidence tier) =>
            OrderingLadder.Reaches(lhs, rhs, new OrderingRung(OrderingDirection.Below, tier));

        OrderingLadder.AchievedTier(lhs, rhs, OrderingDirection.Below)
            .Should().Be(OrderingConfidence.Possible);
        Reaches(OrderingConfidence.Possible).Should().BeTrue();
        Reaches(OrderingConfidence.Nominal).Should().BeFalse();
        Reaches(OrderingConfidence.Certain).Should().BeFalse();
    }

    // ── Containment: a lattice, not a chain ───────────────────────────────────

    [Fact]
    public void EachContainmentRungImpliesTheOnesBelowIt()
    {
        foreach (var (lhs, rhs) in Pairs())
        {
            var because = $"{Describe(lhs)} within {Describe(rhs)}";
            bool? Reaches(ContainmentRung rung) => ContainmentLadder.Reaches(lhs, rhs, rung);

            if (Reaches(ContainmentRung.WhollyWithin) is true)
            {
                Reaches(ContainmentRung.NominalAndUpperWithin).Should().BeTrue(because);
                Reaches(ContainmentRung.NominalAndLowerWithin).Should().BeTrue(because);
            }

            if (Reaches(ContainmentRung.NominalAndUpperWithin) is true)
            {
                Reaches(ContainmentRung.NominalWithin).Should().BeTrue(because);
            }

            if (Reaches(ContainmentRung.NominalAndLowerWithin) is true)
            {
                Reaches(ContainmentRung.NominalWithin).Should().BeTrue(because);
            }

            if (Reaches(ContainmentRung.NominalWithin) is true)
            {
                Reaches(ContainmentRung.Overlaps).Should().BeTrue(because);
            }
        }
    }

    /// <remarks>
    /// The containment half of discovery. Each operator writes its own comparisons and the ladder places them
    /// afterwards, which is what now stops a rung and the operator named after it drifting apart.
    /// </remarks>
    [Theory]
    [MemberData(nameof(ContainmentOperators))]
    public void EachContainmentOperatorsRulesAreDiscoverableAsItsRung(
        IBinaryOperator op, ContainmentRung expected)
    {
        var rules = ((BinaryOperatorBase)op).Rules;

        ContainmentLadder.RungOf(rules).Should().Be(expected);
        rules.Should().Equal(ContainmentLadder.RulesFor(expected));
    }

    public static TheoryData<IBinaryOperator, ContainmentRung> ContainmentOperators()
    {
        var x = new Variable("x", Mass.Kilogram.Dimensionality);

        return new TheoryData<IBinaryOperator, ContainmentRung>
        {
            { new AnyToleranceOverlapOperator { Id = "a", Lhs = x, Rhs = x }, ContainmentRung.Overlaps },
            { new WithinBindingToleranceOperator { Id = "b", Lhs = x, Rhs = x }, ContainmentRung.NominalWithin },
            {
                new PointAndUpperBoundWithinToleranceOperator { Id = "c", Lhs = x, Rhs = x },
                ContainmentRung.NominalAndUpperWithin
            },
            {
                new PointAndLowerBoundWithinToleranceOperator { Id = "d", Lhs = x, Rhs = x },
                ContainmentRung.NominalAndLowerWithin
            },
            { new WhollyWithinToleranceOperator { Id = "e", Lhs = x, Rhs = x }, ContainmentRung.WhollyWithin },
        };
    }

    /// <remarks>
    /// Mutual containment is a quantifier over the ladder rather than a rung of it — the ladder runs one way,
    /// and this asks for it in both. Discovery says so rather than a doc comment claiming it.
    /// </remarks>
    [Fact]
    public void MutualContainmentIsNotARungOfTheContainmentLadder()
    {
        var x = new Variable("x", Mass.Kilogram.Dimensionality);
        var mutual = new MutuallyWithinToleranceOperator { Id = "m", Lhs = x, Rhs = x };

        ContainmentLadder.RungOf(mutual.Rules).Should().BeNull();

        // …but it is exactly the nominal rung asked in both directions.
        var oneWay = ContainmentLadder.RulesFor(ContainmentRung.NominalWithin);
        mutual.Rules.Should().Equal([.. oneWay, .. oneWay.Select(r => r.Mirrored)]);
    }

    /// <remarks>
    /// Why containment has no single ordered <c>Achieved</c>: the two middle rungs are genuinely incomparable,
    /// so no total order over them is honest. Forcing one would have to invent a precedence between "cannot
    /// overshoot" and "cannot undershoot", which are different engineering questions.
    /// </remarks>
    [Fact]
    public void TheTwoMiddleRungsAreIndependentOfEachOther()
    {
        bool? Upper(Measurand l, Measurand r) =>
            ContainmentLadder.Reaches(l, r, ContainmentRung.NominalAndUpperWithin);
        bool? Lower(Measurand l, Measurand r) =>
            ContainmentLadder.Reaches(l, r, ContainmentRung.NominalAndLowerWithin);

        // Subject 10 [9.9, 11] against band 10 [9, 11]: ceiling coincides so upper fits, floor is well inside.
        Upper(M(10, 0.1, 1), M(10, 1, 1)).Should().BeTrue();
        Lower(M(10, 0.1, 1), M(10, 1, 1)).Should().BeTrue();

        // Subject 10 [9, 11.5] against band 10 [9, 11]: floor coincides, ceiling overshoots.
        Lower(M(10, 1, 1.5), M(10, 1, 1)).Should().BeTrue();
        Upper(M(10, 1, 1.5), M(10, 1, 1)).Should().BeFalse();

        // And the mirror image: ceiling fits, floor undershoots.
        Upper(M(10, 1.5, 1), M(10, 1, 1)).Should().BeTrue();
        Lower(M(10, 1.5, 1), M(10, 1, 1)).Should().BeFalse();
    }

    /// <remarks>
    /// The one place the ladder's converse deliberately fails, and the boundary coincidence that really does
    /// arise in practice — a value checked against a spec built from the same figures. Both middle rungs hold,
    /// yet the top does not, because <c>[=}</c> is strict on both bounds and everything below it is not.
    /// Long-standing intended behaviour; pinned here so a later tidy-up of the ladder cannot quietly change it.
    /// </remarks>
    [Fact]
    public void IdenticalIntervalsSatisfyEveryRungExceptTheStrictTop()
    {
        var identical = M(10, 1, 1);
        bool? Reaches(ContainmentRung rung) => ContainmentLadder.Reaches(identical, M(10, 1, 1), rung);

        Reaches(ContainmentRung.Overlaps).Should().BeTrue();
        Reaches(ContainmentRung.NominalWithin).Should().BeTrue();
        Reaches(ContainmentRung.NominalAndUpperWithin).Should().BeTrue();
        Reaches(ContainmentRung.NominalAndLowerWithin).Should().BeTrue();
        Reaches(ContainmentRung.WhollyWithin)
            .Should().BeFalse("an interval is not strictly inside a copy of itself");
    }

    [Fact]
    public void TouchingIntervalsOverlapButContainNothing()
    {
        // [9, 10] and [10, 11] share exactly the point 10.
        bool? Reaches(ContainmentRung rung) =>
            ContainmentLadder.Reaches(M(9.5, 0.5, 0.5), M(10.5, 0.5, 0.5), rung);

        Reaches(ContainmentRung.Overlaps).Should().BeTrue("overlap is non-strict, so touching counts");
        Reaches(ContainmentRung.NominalWithin).Should().BeFalse();
        Reaches(ContainmentRung.WhollyWithin).Should().BeFalse();
    }

    /// <remarks>
    /// Overlap is the rung where the asymmetry between subject and band genuinely disappears, which is why
    /// <c>AnyToleranceOverlapOperator</c> is the commutative one.
    /// </remarks>
    [Fact]
    public void OverlapIsSymmetricWhileTheHigherRungsAreNot()
    {
        foreach (var (lhs, rhs) in Pairs())
        {
            ContainmentLadder.Reaches(lhs, rhs, ContainmentRung.Overlaps)
                .Should().Be(
                    ContainmentLadder.Reaches(rhs, lhs, ContainmentRung.Overlaps),
                    Describe(lhs) + "/" + Describe(rhs));
        }

        // Narrow inside wide is contained; wide inside narrow is not.
        ContainmentLadder.Reaches(M(10, 0.5, 0.5), M(10, 1, 1), ContainmentRung.WhollyWithin)
            .Should().BeTrue();
        ContainmentLadder.Reaches(M(10, 1, 1), M(10, 0.5, 0.5), ContainmentRung.WhollyWithin)
            .Should().BeFalse();
    }

    // ── The two operators that stay off the ladder ────────────────────────────

    /// <remarks>
    /// <c>&lt;^</c> and <c>&gt;v</c> compare a derived statistic of each side — ceiling against ceiling, floor
    /// against floor — rather than asking anything about the quantities' relationship. Neither is a tier of
    /// either ladder, which is why both keep their own arithmetic.
    /// </remarks>
    [Fact]
    public void TheStatisticComparisonsAreNotTiersOfEitherLadder()
    {
        var subject = M(9.5, 0.5, 1);  // [9, 10.5]
        var band = M(10.5, 0.5, 1);    // [10, 11.5]

        var upperLess = new UpperBoundsLessThanOperator
        {
            Id = "u", Lhs = new Variable("x", Mass.Kilogram.Dimensionality),
            Rhs = new Variable("y", Mass.Kilogram.Dimensionality)
        };

        // Ceilings: 10.5 < 11.5, so it holds — while the subject's nominal value sits below the band entirely,
        // and the ordering ladder gets no further than "nominal". Neither ladder has a rung in this position.
        upperLess.IsSatisfiedGiven(subject, band).Should().BeTrue();

        ContainmentLadder.Reaches(subject, band, ContainmentRung.Overlaps).Should().BeTrue();
        ContainmentLadder.Reaches(subject, band, ContainmentRung.NominalWithin)
            .Should().BeFalse("9.5 is below the band's floor of 10");

        OrderingLadder.AchievedTier(subject, band, OrderingDirection.Below)
            .Should().Be(OrderingConfidence.Nominal);
    }
}
