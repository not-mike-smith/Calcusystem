using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.BinaryOperators;

/// <summary>
/// The seam separating <i>what a relationship asserts</i> from <i>where its values came from</i>. Every operator
/// implements the predicate over two supplied values; the base class implements resolving both sides once, so
/// the fourteen agree on the null case by construction rather than by fourteen copies of one guard.
/// </summary>
public class VerdictSeamTests
{
    private static Variable Valued(double kmsValue, double relErr = 0) =>
        new("x", Mass.Kilogram.Quantity(kmsValue).Measurand(SymmetricUncertainty.FromRelative(relErr)));

    private static Variable Unset() => new("x", Mass.Kilogram.Dimensionality);

    private static Measurand Kg(double kmsValue, double relErr = 0) =>
        Mass.Kilogram.Quantity(kmsValue).Measurand(SymmetricUncertainty.FromRelative(relErr));

    /// <summary>One of each operator, all over the same two operands.</summary>
    private static IEnumerable<IBinaryOperator> AllOperators(IExpression lhs, IExpression rhs) =>
    [
        new DefinitelyLessThanOperator { Id = "a", Lhs = lhs, Rhs = rhs },
        new UpperBoundsLessThanOperator { Id = "b", Lhs = lhs, Rhs = rhs },
        new NominallyLessThanOperator { Id = "c", Lhs = lhs, Rhs = rhs },
        new DefinitelyGreaterThanOperator { Id = "d", Lhs = lhs, Rhs = rhs },
        new LowerBoundsGreaterThanOperator { Id = "e", Lhs = lhs, Rhs = rhs },
        new NominallyGreaterThanOperator { Id = "f", Lhs = lhs, Rhs = rhs },
        new WithinBindingToleranceOperator { Id = "g", Lhs = lhs, Rhs = rhs },
        new PointAndUpperBoundWithinToleranceOperator { Id = "h", Lhs = lhs, Rhs = rhs },
        new PointAndLowerBoundWithinToleranceOperator { Id = "i", Lhs = lhs, Rhs = rhs },
        new MutuallyWithinToleranceOperator { Id = "j", Lhs = lhs, Rhs = rhs },
        new AnyToleranceOverlapOperator { Id = "k", Lhs = lhs, Rhs = rhs },
        new WhollyWithinToleranceOperator { Id = "l", Lhs = lhs, Rhs = rhs },
        new EqualityOperator(AgreementRule.Nominal, SolvingRole.Requirement) { Id = "m", Lhs = lhs, Rhs = rhs },
        new SimpleComparison(new ComparisonRule(Landmark.Nominal, MustBe.LessThan, Landmark.LowerBound))
            { Id = "n", Lhs = lhs, Rhs = rhs },
    ];

    [Fact]
    public void ThereAreFourteenOperatorsAndTheListIsComplete()
    {
        // Guards the sweeps below: a fifteenth operator that skipped this list would be silently untested.
        var covered = AllOperators(Valued(1), Valued(1)).Select(o => o.GetType()).ToHashSet();

        var declared = typeof(DefinitelyLessThanOperator).Assembly.GetTypes()
            .Where(t => ! t.IsAbstract && typeof(IBinaryOperator).IsAssignableFrom(t))
            .ToHashSet();

        covered.Should().BeEquivalentTo(declared);
        covered.Should().HaveCount(14);
    }

    /// <summary>
    /// A symbol read from the other side: the characters reversed, each mapped to its mirror image.
    /// </summary>
    /// <remarks>
    /// Not plain string reversal, which would call <c>·&lt;·</c> a palindrome and so declare it commutative.
    /// This is <c>ComparisonRule.Mirrored</c> lifted to notation — the corners swap hands, the relations flip,
    /// and the statistic glyphs stay put.
    /// </remarks>
    private static string MirrorReverse(string symbol)
    {
        const string Fixed = "·=≈≠?∅";

        return new string(symbol.Reverse().Select(c => c switch
        {
            '⌜' => '⌝', '⌝' => '⌜',
            '⌞' => '⌟', '⌟' => '⌞',
            '{' => '}', '}' => '{',
            '[' => ']', ']' => '[',
            '<' => '>', '>' => '<',
            '≤' => '≥', '≥' => '≤',
            _ => Fixed.Contains(c) ? c : throw new ArgumentException($"No mirror for '{c}' in {symbol}"),
        }).ToArray());
    }

    /// <remarks>
    /// <para>
    /// The notation earns its keep here. A commutative relationship says the same thing read from either side,
    /// so its symbol must too — and a non-commutative one must not, or the symbol would claim a symmetry the
    /// operator does not have.
    /// </para>
    /// <para>
    /// This is what retired <c>≃=</c> and <c>≈=</c>: a trailing family marker reads the same way round from one
    /// side only, which is exactly what a commutative relation should never do. It also caught
    /// <see cref="SimpleComparison"/> declaring itself non-commutative when configured with <c>·=·</c>, which
    /// is commutative by any reading.
    /// </para>
    /// </remarks>
    [Fact]
    public void ACommutativeOperatorIsExactlyOneWhoseSymbolReadsTheSameBothWays()
    {
        foreach (var op in AllOperators(Valued(1), Valued(1)))
        {
            (MirrorReverse(op.Symbol) == op.Symbol).Should().Be(
                op.IsCommutative,
                $"{op.GetType().Name} spells {op.Symbol}, which mirrors to {MirrorReverse(op.Symbol)}");
        }
    }

    /// <remarks>
    /// The rule the equality family is built on, stated as a test rather than as a convention: <b>each symbol
    /// is the symbol of the operator asserting the same condition, with an <c>=</c> inserted at its centre.</b>
    /// Nothing else pins it — <c>==</c> would satisfy the palindrome invariant and the uniqueness check just as
    /// well, which is exactly why the scheme needs asserting rather than assuming.
    /// <para>
    /// Centred insertion is also what keeps the family from breaking commutativity: <c>=</c> is its own mirror
    /// and the centre is the fixed point of mirror-reversal, so a palindrome stays one.
    /// </para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(EqualityCounterparts))]
    public void AnEqualitySymbolIsItsConditionsSymbolWithACentredEquals(
        AgreementRule rule, string counterpart)
    {
        var op = new EqualityOperator(rule, SolvingRole.Requirement) { Id = "eq", Lhs = Valued(1), Rhs = Valued(1) };

        op.Symbol.Should().Be(counterpart.Insert(counterpart.Length / 2, "="));
    }

    public static TheoryData<AgreementRule, string> EqualityCounterparts()
    {
        var x = new Variable("x", Mass.Kilogram.Dimensionality);

        return new TheoryData<AgreementRule, string>
        {
            // The condition is one rule, so its counterpart is that rule's own generated symbol.
            {
                AgreementRule.Nominal,
                new ComparisonRule(Landmark.Nominal, MustBe.EqualTo, Landmark.Nominal).Symbol
            },
            {
                AgreementRule.Mutual,
                new MutuallyWithinToleranceOperator { Id = "m", Lhs = x, Rhs = x }.Symbol
            },
            {
                AgreementRule.Overlapping,
                new AnyToleranceOverlapOperator { Id = "o", Lhs = x, Rhs = x }.Symbol
            },
        };
    }

    /// <remarks>
    /// Swept separately because the operator list holds one equality, at one agreement rule — so the sweep
    /// above never sees the other two symbols. That gap let <c>≃=</c> and <c>≈=</c> survive the invariant that
    /// was written to retire them, and was found by mutating them back.
    /// </remarks>
    [Theory]
    [InlineData(AgreementRule.Nominal)]
    [InlineData(AgreementRule.Mutual)]
    [InlineData(AgreementRule.Overlapping)]
    public void EveryAgreementRuleSpellsACommutativeSymbol(AgreementRule rule)
    {
        var op = new EqualityOperator(rule, SolvingRole.Requirement) { Id = "eq", Lhs = Valued(1), Rhs = Valued(1) };

        op.IsCommutative.Should().BeTrue("an equality reads the same from either side");
        MirrorReverse(op.Symbol).Should().Be(op.Symbol, $"{rule} spells {op.Symbol}");
    }

    /// <remarks>
    /// The general form covers both cases, so it is swept separately over every rule rather than in the one
    /// configuration the operator list happens to hold.
    /// </remarks>
    [Fact]
    public void ASimpleComparisonIsCommutativeExactlyWhenItsRuleHasNoSide()
    {
        var x = Valued(1);

        foreach (var landmark in Enum.GetValues<Landmark>())
        foreach (var mask in Enum.GetValues<MustBe>())
        foreach (var other in Enum.GetValues<Landmark>())
        {
            // `None` accepts no outcome and is refused at construction — see ComparisonRuleTests.
            if (mask is MustBe.Impossible) continue;

            var op = new SimpleComparison(new ComparisonRule(landmark, mask, other))
            {
                Id = "s", Lhs = x, Rhs = x,
            };

            (MirrorReverse(op.Symbol) == op.Symbol).Should().Be(op.IsCommutative, op.Symbol);
            op.IsCommutative.Should().Be(
                landmark == other
                    && mask is MustBe.EqualTo or MustBe.InequalTo or MustBe.Comparable,
                op.Symbol);
        }
    }

    /// <remarks>
    /// <para>
    /// `Symbol` is how a relationship identifies itself to a reader, and `OPERATORS.md` documents each operator
    /// under its symbol — both of which quietly assume no two share one.
    /// </para>
    /// <para>
    /// <see cref="SimpleComparison"/> is excepted, and has to be: it can be configured to spell any of the nine
    /// landmark comparisons, six of which have named types. Its symbol coinciding with one of theirs is not a
    /// collision but an identity — the two assert the same rule — so nothing is lost by a report that cannot
    /// tell them apart.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryNamedOperatorHasItsOwnSymbol()
    {
        var symbols = AllOperators(Valued(1), Valued(1))
            .Where(o => o is not SimpleComparison)
            .Select(o => o.Symbol)
            .ToList();

        symbols.Should().OnlyHaveUniqueItems();
        symbols.Should().NotContain(s => string.IsNullOrWhiteSpace(s));
    }

    /// <remarks>
    /// The predicate takes values, so it cannot consult the model — which is what lets a calculation judge a
    /// relationship at trial values, and what stops it re-walking subgraphs it has already computed.
    /// </remarks>
    [Theory]
    [InlineData(1, 0.0, 100, 0.0)]
    [InlineData(100, 0.0, 1, 0.0)]
    [InlineData(10, 0.5, 10, 0.1)]
    [InlineData(10, 0.1, 10, 0.5)]
    public void EveryOperatorJudgesTheSuppliedValuesRatherThanItsOperands(
        double lhsValue, double lhsErr, double rhsValue, double rhsErr)
    {
        // Every operator is built over operands saying 3 kg vs 7 kg, then asked about entirely different values.
        // Its answer must match the same operator built over operands that really do hold those values — which
        // is the property that lets a calculation judge at trial values, and stops it re-walking the graph.
        var decoys = AllOperators(Valued(3), Valued(7)).ToList();
        var honest = AllOperators(Valued(lhsValue, lhsErr), Valued(rhsValue, rhsErr)).ToList();

        foreach (var (decoy, reference) in decoys.Zip(honest))
        {
            reference.IsSatisfied()
                .Should().Be(decoy.IsSatisfiedGiven(Kg(lhsValue, lhsErr), Kg(rhsValue, rhsErr)),
                             decoy.Symbol);
        }
    }

    /// <remarks>
    /// Implemented once on the base class rather than thirteen times. Before the seam this guard was copied into
    /// every operator, which is thirteen chances for one of them to differ.
    /// </remarks>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void EveryOperatorAnswersUnknownWhenASideDoesNotResolve(bool lhsUnset, bool rhsUnset)
    {
        var lhs = lhsUnset ? Unset() : Valued(1);
        var rhs = rhsUnset ? Unset() : Valued(1);

        foreach (var op in AllOperators(lhs, rhs))
        {
            op.IsSatisfied().Should().BeNull($"{op.Symbol} cannot judge an unresolved side");
        }
    }

    /// <remarks>
    /// Optional parameters on the existing method rather than an overload — two overloads with all-optional
    /// trailing parameters make the no-argument call ambiguous.
    /// </remarks>
    [Fact]
    public void IsSatisfiedTakesOverridesAndPrefersThemToStoredValues()
    {
        var measured = Valued(1);
        var limit = Valued(10);
        var op = new DefinitelyLessThanOperator { Id = "lt", Lhs = measured, Rhs = limit };

        op.IsSatisfied().Should().BeTrue();

        op.IsSatisfied(new Dictionary<Variable, Measurand> { [measured] = Kg(50) })
            .Should().BeFalse();

        // The model is untouched, exactly as `Calculate`'s overrides leave it.
        measured.Value!.KmsValue.Should().BeApproximately(1, 1e-9);
    }

    [Fact]
    public void IsSatisfiedCanResolveAnUnsetOperandFromAnOverride()
    {
        var measured = Unset();
        var op = new DefinitelyLessThanOperator { Id = "lt", Lhs = measured, Rhs = Valued(10) };

        op.IsSatisfied().Should().BeNull();
        op.IsSatisfied(new Dictionary<Variable, Measurand> { [measured] = Kg(1) }).Should().BeTrue();
    }

    // ── Subject / Criterion ───────────────────────────────────────────────────

    /// <remarks>
    /// Twelve of the thirteen can only ever be requirements, and by construction their <c>Lhs</c> is the value
    /// under test — which is what <c>OPERATORS.md</c> has always documented.
    /// </remarks>
    [Fact]
    public void ARequirementJudgesItsLhsAgainstItsRhs()
    {
        var lhs = Valued(1);
        var rhs = Valued(10);

        foreach (var op in AllOperators(lhs, rhs).Where(o => o.SolvingRole is SolvingRole.Requirement))
        {
            op.Subject.Should().Be(lhs, $"{op.Symbol} tests its left operand");
            op.Criterion.Should().Be(rhs, $"{op.Symbol} tests against its right operand");
        }
    }

    /// <remarks>
    /// Neither side of <c>T_eos == T_path</c> is the one being judged, so labelling one of them would invent an
    /// authority the model never asserted — and it is exactly that absence which makes a failure an
    /// inconsistency rather than a violation.
    /// </remarks>
    [Theory]
    [InlineData(SolvingRole.Equation)]
    [InlineData(SolvingRole.Coherence)]
    public void ADeterminingRelationshipHasNeitherASubjectNorACriterion(SolvingRole role)
    {
        var op = new EqualityOperator(AgreementRule.Nominal, role) { Id = "eq", Lhs = Valued(1), Rhs = Valued(1) };

        op.Subject.Should().BeNull();
        op.Criterion.Should().BeNull();
    }

    /// <remarks>
    /// The same operator type lands on both answers, so the labelling follows the role rather than the class —
    /// an equality checking a measurement against a design figure does have an authority between its sides.
    /// </remarks>
    [Fact]
    public void AnEqualityActingAsARequirementDoesHaveThem()
    {
        var lhs = Valued(1);
        var rhs = Valued(1);
        var op = new EqualityOperator(AgreementRule.Nominal, SolvingRole.Requirement)
        {
            Id = "eq", Lhs = lhs, Rhs = rhs
        };

        op.Subject.Should().Be(lhs);
        op.Criterion.Should().Be(rhs);
    }

    /// <remarks>
    /// Two ways of deriving the same taxonomy, which is the best evidence the model is right. Worth re-checking
    /// when n-ary coherence arrives — a group with no two distinguished sides has no criterion either.
    /// </remarks>
    [Theory]
    [InlineData(SolvingRole.Requirement)]
    [InlineData(SolvingRole.Equation)]
    [InlineData(SolvingRole.Coherence)]
    public void HavingACriterionIsExactlyBeingARequirement(SolvingRole role)
    {
        var equality = new EqualityOperator(AgreementRule.Nominal, role) { Id = "eq", Lhs = Valued(1), Rhs = Valued(1) };

        (equality.Criterion is not null).Should().Be(role is SolvingRole.Requirement);
        (equality.Subject is not null).Should().Be(equality.Criterion is not null);

        foreach (var op in AllOperators(Valued(1), Valued(1)))
        {
            (op.Criterion is not null).Should().Be(op.SolvingRole is SolvingRole.Requirement, op.Symbol);
            (op.Criterion is not null).Should().Be(! op.IsDetermining, op.Symbol);
        }
    }
}
