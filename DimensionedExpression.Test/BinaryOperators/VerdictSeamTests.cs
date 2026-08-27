using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.BinaryOperators;

/// <summary>
/// The seam separating <i>what a relationship asserts</i> from <i>where its values came from</i>. Every operator
/// implements the predicate over two supplied values; the base class implements resolving both sides once, so
/// the thirteen agree on the null case by construction rather than by thirteen copies of one guard.
/// </summary>
public class VerdictSeamTests
{
    private static Variable Bound(double kmsValue, double relErr = 0) =>
        new("x", Mass.Kilogram.Quantity(kmsValue).Measurand(SymmetricUncertainty.FromRelErr(relErr)));

    private static Variable Unbound() => new("x", Mass.Kilogram.Dimensionality);

    private static Measurand Kg(double kmsValue, double relErr = 0) =>
        Mass.Kilogram.Quantity(kmsValue).Measurand(SymmetricUncertainty.FromRelErr(relErr));

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
        new EqualityOperator(new AlwaysEqual(), SolvingRole.Requirement) { Id = "m", Lhs = lhs, Rhs = rhs },
    ];

    [Fact]
    public void ThereAreThirteenOperatorsAndTheListIsComplete()
    {
        // Guards the sweeps below: a fourteenth operator that skipped this list would be silently untested.
        var covered = AllOperators(Bound(1), Bound(1)).Select(o => o.GetType()).ToHashSet();

        var declared = typeof(DefinitelyLessThanOperator).Assembly.GetTypes()
            .Where(t => ! t.IsAbstract && typeof(IBinaryOperator).IsAssignableFrom(t))
            .ToHashSet();

        covered.Should().BeEquivalentTo(declared);
        covered.Should().HaveCount(13);
    }

    /// <remarks>
    /// `Symbol` is how a relationship identifies itself to a reader, and `OPERATORS.md` documents each operator
    /// under its symbol — both of which quietly assume no two share one.
    /// </remarks>
    [Fact]
    public void EveryOperatorHasItsOwnSymbol()
    {
        var symbols = AllOperators(Bound(1), Bound(1)).Select(o => o.Symbol).ToList();

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
        var decoys = AllOperators(Bound(3), Bound(7)).ToList();
        var honest = AllOperators(Bound(lhsValue, lhsErr), Bound(rhsValue, rhsErr)).ToList();

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
    public void EveryOperatorAnswersUnknownWhenASideDoesNotResolve(bool lhsUnbound, bool rhsUnbound)
    {
        var lhs = lhsUnbound ? Unbound() : Bound(1);
        var rhs = rhsUnbound ? Unbound() : Bound(1);

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
        var measured = Bound(1);
        var limit = Bound(10);
        var op = new DefinitelyLessThanOperator { Id = "lt", Lhs = measured, Rhs = limit };

        op.IsSatisfied().Should().BeTrue();

        op.IsSatisfied(new Dictionary<Variable, Measurand> { [measured] = Kg(50) })
            .Should().BeFalse();

        // The model is untouched, exactly as `Calculate`'s overrides leave it.
        measured.Value!.KmsValue.Should().BeApproximately(1, 1e-9);
    }

    [Fact]
    public void IsSatisfiedCanResolveAnUnboundOperandFromAnOverride()
    {
        var measured = Unbound();
        var op = new DefinitelyLessThanOperator { Id = "lt", Lhs = measured, Rhs = Bound(10) };

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
        var lhs = Bound(1);
        var rhs = Bound(10);

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
        var op = new EqualityOperator(new AlwaysEqual(), role) { Id = "eq", Lhs = Bound(1), Rhs = Bound(1) };

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
        var lhs = Bound(1);
        var rhs = Bound(1);
        var op = new EqualityOperator(new AlwaysEqual(), SolvingRole.Requirement)
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
        var equality = new EqualityOperator(new AlwaysEqual(), role) { Id = "eq", Lhs = Bound(1), Rhs = Bound(1) };

        (equality.Criterion is not null).Should().Be(role is SolvingRole.Requirement);
        (equality.Subject is not null).Should().Be(equality.Criterion is not null);

        foreach (var op in AllOperators(Bound(1), Bound(1)))
        {
            (op.Criterion is not null).Should().Be(op.SolvingRole is SolvingRole.Requirement, op.Symbol);
            (op.Criterion is not null).Should().Be(! op.IsDetermining, op.Symbol);
        }
    }

    private sealed class AlwaysEqual : IEqualityEstimating
    {
        public bool AreEqual(Measurand lhs, Measurand rhs) => true;
    }
}
