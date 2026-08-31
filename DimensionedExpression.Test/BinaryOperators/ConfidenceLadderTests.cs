using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.BinaryOperators;

/// <summary>
/// The ladder underneath the named operators: under uncertainty a comparison has several nested answers, and one
/// pass computes all of them. These pin the nesting, the one place it deliberately stops, and — most importantly
/// — that routing the operators through it changed no answer anywhere.
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
    /// The real proof the ladder changed nothing. The pre-existing suite is 65 hand-picked cases; this is every
    /// operator against its original formula over 2,025 pairs built to land on exact boundary coincidences,
    /// which is where a rewrite of interval comparisons would actually go wrong.
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

    [Fact]
    public void EachOrderingTierImpliesTheOneBelowIt()
    {
        foreach (var (lhs, rhs) in Pairs())
        {
            var ladder = OrderingLadder.Evaluate(lhs, rhs);

            if (ladder.Certain) ladder.Nominal.Should().BeTrue($"certain implies nominal for {Describe(lhs)}/{Describe(rhs)}");
            if (ladder.Nominal) ladder.Possible.Should().BeTrue($"nominal implies possible for {Describe(lhs)}/{Describe(rhs)}");
        }
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
        OrderingLadder.Evaluate(M(lhsValue, lhsLower, lhsUpper), M(rhsValue, rhsLower, rhsUpper))
            .Achieved.Should().Be(expected);
    }

    [Fact]
    public void ReachesAsksForAtLeastATier()
    {
        // 10 ± 1 against 10.5 ± 1: nominally ordered, intervals overlap.
        var ladder = OrderingLadder.Evaluate(M(10, 1, 1), M(10.5, 1, 1));

        ladder.Reaches(OrderingConfidence.Possible).Should().BeTrue();
        ladder.Reaches(OrderingConfidence.Nominal).Should().BeTrue();
        ladder.Reaches(OrderingConfidence.Certain).Should().BeFalse();
    }

    /// <remarks>
    /// The tier no named operator ever asked for, and the reason the ladder is worth having: a modeller who
    /// writes <c>&lt;~</c> and gets <c>false</c> currently cannot tell "comfortably the other way round" from
    /// "a hair's breadth away, and the uncertainty covers it".
    /// </remarks>
    [Fact]
    public void PossibleIsReachableWithoutNominal()
    {
        // 10.5 ± 1 vs 10 ± 1 — nominally greater, but the intervals overlap heavily.
        var ladder = OrderingLadder.Evaluate(M(10.5, 1, 1), M(10, 1, 1));

        ladder.Possible.Should().BeTrue();
        ladder.Nominal.Should().BeFalse();
        ladder.Certain.Should().BeFalse();
    }

    // ── Containment: a lattice, not a chain ───────────────────────────────────

    [Fact]
    public void EachContainmentRungImpliesTheOnesBelowIt()
    {
        foreach (var (lhs, rhs) in Pairs())
        {
            var ladder = ContainmentLadder.Evaluate(lhs, rhs);
            var because = $"{Describe(lhs)} within {Describe(rhs)}";

            if (ladder.WhollyWithin)
            {
                ladder.NominalAndUpperWithin.Should().BeTrue(because);
                ladder.NominalAndLowerWithin.Should().BeTrue(because);
            }

            if (ladder.NominalAndUpperWithin) ladder.NominalWithin.Should().BeTrue(because);
            if (ladder.NominalAndLowerWithin) ladder.NominalWithin.Should().BeTrue(because);
            if (ladder.NominalWithin) ladder.Overlaps.Should().BeTrue(because);
        }
    }

    /// <remarks>
    /// Why containment has no single ordered <c>Achieved</c>: the two middle rungs are genuinely incomparable,
    /// so no total order over them is honest. Forcing one would have to invent a precedence between "cannot
    /// overshoot" and "cannot undershoot", which are different engineering questions.
    /// </remarks>
    [Fact]
    public void TheTwoMiddleRungsAreIndependentOfEachOther()
    {
        // Subject 10 [9.9, 11] against band 10 [9, 11]: ceiling coincides so upper fits, floor is well inside.
        var upperOnly = ContainmentLadder.Evaluate(M(10, 0.1, 1), M(10, 1, 1));
        upperOnly.NominalAndUpperWithin.Should().BeTrue();
        upperOnly.NominalAndLowerWithin.Should().BeTrue();

        // Subject 10 [9, 11.5] against band 10 [9, 11]: floor coincides, ceiling overshoots.
        var lowerOnly = ContainmentLadder.Evaluate(M(10, 1, 1.5), M(10, 1, 1));
        lowerOnly.NominalAndLowerWithin.Should().BeTrue();
        lowerOnly.NominalAndUpperWithin.Should().BeFalse();

        // And the mirror image: ceiling fits, floor undershoots.
        var upperFits = ContainmentLadder.Evaluate(M(10, 1.5, 1), M(10, 1, 1));
        upperFits.NominalAndUpperWithin.Should().BeTrue();
        upperFits.NominalAndLowerWithin.Should().BeFalse();
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
        var ladder = ContainmentLadder.Evaluate(M(10, 1, 1), M(10, 1, 1));

        ladder.Overlaps.Should().BeTrue();
        ladder.NominalWithin.Should().BeTrue();
        ladder.NominalAndUpperWithin.Should().BeTrue();
        ladder.NominalAndLowerWithin.Should().BeTrue();
        ladder.WhollyWithin.Should().BeFalse("an interval is not strictly inside a copy of itself");
    }

    [Fact]
    public void TouchingIntervalsOverlapButContainNothing()
    {
        // [9, 10] and [10, 11] share exactly the point 10.
        var ladder = ContainmentLadder.Evaluate(M(9.5, 0.5, 0.5), M(10.5, 0.5, 0.5));

        ladder.Overlaps.Should().BeTrue("overlap is non-strict, so touching counts");
        ladder.NominalWithin.Should().BeFalse();
        ladder.WhollyWithin.Should().BeFalse();
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
            ContainmentLadder.Evaluate(lhs, rhs).Overlaps
                .Should().Be(ContainmentLadder.Evaluate(rhs, lhs).Overlaps, Describe(lhs) + "/" + Describe(rhs));
        }

        // Narrow inside wide is contained; wide inside narrow is not.
        ContainmentLadder.Evaluate(M(10, 0.5, 0.5), M(10, 1, 1)).WhollyWithin.Should().BeTrue();
        ContainmentLadder.Evaluate(M(10, 1, 1), M(10, 0.5, 0.5)).WhollyWithin.Should().BeFalse();
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

        var containment = ContainmentLadder.Evaluate(subject, band);
        containment.Overlaps.Should().BeTrue();
        containment.NominalWithin.Should().BeFalse("9.5 is below the band's floor of 10");

        OrderingLadder.Evaluate(subject, band).Achieved.Should().Be(OrderingConfidence.Nominal);
    }
}
