using Calcusystem.Analysis.Extensions;
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

/// <summary>
/// A calculation reports on the model's relationships as well as its values. These pin what a verdict is a
/// function of, that a relationship which could not be judged says so rather than passing quietly, and that a
/// failed requirement and a failed equation are reported as different findings.
/// </summary>
public class RelationshipOutcomeTests
{
    private static Variable Bound(string symbol, double kmsValue, double relErr = 0) =>
        new(symbol,
            new Quantity(kmsValue, Dimensionality.Length).Measurand(SymmetricUncertainty.FromRelErr(relErr)),
            symbol);

    /// <summary>A length whose uncertainty is unbounded upward, so its ceiling has no comparable value.</summary>
    private static Variable NoCeiling(string symbol, double kmsValue) =>
        new(symbol,
            new Quantity(kmsValue, Dimensionality.Length).Measurand(
                AsymmetricUncertainty.FromAbsErr(
                    new Quantity(double.PositiveInfinity, Dimensionality.Length),
                    new Quantity(0, Dimensionality.Length))),
            symbol);

    private static Variable Unbound(string symbol) =>
        new(symbol, Dimensionality.Length, symbol);

    private static Measurand Length(double kmsValue) =>
        new Quantity(kmsValue, Dimensionality.Length).Measurand(SymmetricUncertainty.FromRelErr(0));

    private static ExpressionSystem SystemOf(params object[] members)
    {
        var system = ExpressionSystem.Create("outcomes", "");
        foreach (var member in members)
        {
            switch (member)
            {
                case IBinaryOperator relationship: system.Add(relationship); break;
                case IExpression expression: system.Add(expression); break;
                default: throw new ArgumentException($"not a system member: {member}");
            }
        }

        return system;
    }

    /// <remarks>
    /// The reason the seam exists. <c>IsSatisfied()</c> resolves its own operands from the stored model, so a
    /// calculation run at trial values that delegated to it would report a verdict about values it was
    /// explicitly told to ignore — wrong, and wrong silently.
    /// </remarks>
    [Fact]
    public void AVerdictIsJudgedOnTheOverridesRatherThanTheStoredValues()
    {
        var measured = Bound("measured", 5);
        var limit = Bound("limit", 10);
        var under = new DefinitelyLessThanOperator { Id = "under", Lhs = measured, Rhs = limit };
        var system = SystemOf(under);

        system.Calculate().Outcomes.Single().IsSatisfied.Should().BeTrue();

        var overridden = system.Calculate(
            new Dictionary<Variable, Measurand> { [measured] = Length(50) });

        overridden.Outcomes.Single().IsSatisfied.Should().BeFalse();
        overridden.Violations.Should().ContainSingle().Which.Relationship.Should().Be(under);

        // And the operator asked on its own still answers about the model, which is what it is for.
        under.IsSatisfied().Should().BeTrue();
    }

    /// <remarks>
    /// The values on the outcome are the ones the verdict was reached on, not a fresh read. Without this a
    /// report could show a passing check beside the numbers that would have failed it.
    /// </remarks>
    [Fact]
    public void TheOutcomeCarriesTheValuesItWasJudgedOn()
    {
        var measured = Bound("measured", 5);
        var limit = Bound("limit", 10);
        var system = SystemOf(new DefinitelyLessThanOperator { Id = "under", Lhs = measured, Rhs = limit });

        var outcome = system
            .Calculate(new Dictionary<Variable, Measurand> { [measured] = Length(50) })
            .Outcomes.Single();

        outcome.Lhs!.KmsValue.Should().BeApproximately(50, 1e-9);
        outcome.Rhs!.KmsValue.Should().BeApproximately(10, 1e-9);
    }

    [Fact]
    public void ASatisfiedRelationshipIsNeitherAViolationNorAnInconsistency()
    {
        var system = SystemOf(new DefinitelyLessThanOperator
        {
            Id = "under", Lhs = Bound("measured", 5), Rhs = Bound("limit", 10)
        });

        var calc = system.Calculate();

        calc.Outcomes.Should().ContainSingle().Which.IsSatisfied.Should().BeTrue();
        calc.Violations.Should().BeEmpty();
        calc.Inconsistencies.Should().BeEmpty();
        calc.Undetermined.Should().BeEmpty();
        calc.AllRelationshipsHold.Should().BeTrue();
    }

    /// <remarks>
    /// The taxonomy split. A requirement names an authority to judge against, so a failure is attributable to
    /// the subject; an equation names none, so the finding is against the model rather than against a side.
    /// </remarks>
    [Fact]
    public void AFailedRequirementIsAViolationAndAFailedEquationIsAnInconsistency()
    {
        var measured = Bound("measured", 50);
        var limit = Bound("limit", 10);
        var equation = new EqualityOperator(AgreementRule.Nominal, SolvingRole.Equation)
        {
            Id = "eq", Lhs = measured, Rhs = limit
        };
        var requirement = new DefinitelyLessThanOperator { Id = "under", Lhs = measured, Rhs = limit };
        var system = SystemOf(requirement, equation);

        var calc = system.Calculate();

        calc.Violations.Select(o => o.Relationship.Id).Should().Equal("under");
        calc.Inconsistencies.Select(o => o.Relationship.Id).Should().Equal("eq");
        calc.AllRelationshipsHold.Should().BeFalse();
    }

    /// <remarks>
    /// Coherence sits with equations, not with requirements: two independently computed routes to one quantity
    /// have no authority between them either, so a disagreement is a finding about the model.
    /// </remarks>
    [Fact]
    public void AFailedCoherenceAssertionIsAnInconsistency()
    {
        var system = SystemOf(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Coherence)
        {
            Id = "routes-agree", Lhs = Bound("t_eos", 300), Rhs = Bound("t_path", 305)
        });

        var calc = system.Calculate();

        calc.Inconsistencies.Select(o => o.Relationship.Id).Should().Equal("routes-agree");
        calc.Violations.Should().BeEmpty();
    }

    /// <remarks>
    /// An equality can still be a requirement — a measurement checked against a design figure has an authority
    /// between its sides. So the split follows the role, not the operator type.
    /// </remarks>
    [Fact]
    public void AFailedEqualityActingAsARequirementIsAViolation()
    {
        var system = SystemOf(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Requirement)
        {
            Id = "as-designed", Lhs = Bound("measured", 5), Rhs = Bound("design", 6)
        });

        var calc = system.Calculate();

        calc.Violations.Select(o => o.Relationship.Id).Should().Equal("as-designed");
        calc.Inconsistencies.Should().BeEmpty();
    }

    /// <remarks>
    /// A relationship missing from the report is indistinguishable from one that passed, which is the reading
    /// error worth designing against: an engineer scanning a clean result must be able to see that a check
    /// never ran.
    /// </remarks>
    [Fact]
    public void ARelationshipWhoseSideDidNotResolveIsUndeterminedRatherThanFailed()
    {
        var unbound = Unbound("measured");
        var system = SystemOf(new DefinitelyLessThanOperator
        {
            Id = "under", Lhs = unbound, Rhs = Bound("limit", 10)
        });

        var calc = system.Calculate();

        var outcome = calc.Outcomes.Should().ContainSingle().Subject;
        outcome.IsSatisfied.Should().BeNull();
        outcome.Lhs.Should().BeNull();
        outcome.Rhs!.KmsValue.Should().BeApproximately(10, 1e-9);

        calc.Undetermined.Should().ContainSingle();
        calc.Violations.Should().BeEmpty();
        calc.Inconsistencies.Should().BeEmpty();

        // Nothing failed, so nothing is reported as failing — but the calculation is not complete either.
        calc.AllRelationshipsHold.Should().BeTrue();
        calc.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void EveryRelationshipAppearsExactlyOnceIncludingTheUndeterminedOnes()
    {
        var measured = Bound("measured", 50);
        var limit = Bound("limit", 10);
        var unbound = Unbound("unknown");

        var system = SystemOf(
            new DefinitelyLessThanOperator { Id = "fails", Lhs = measured, Rhs = limit },
            new DefinitelyGreaterThanOperator { Id = "holds", Lhs = measured, Rhs = limit },
            new DefinitelyLessThanOperator { Id = "unjudgeable", Lhs = unbound, Rhs = limit });

        var calc = system.Calculate();

        calc.Outcomes.Select(o => o.Relationship.Id)
            .Should().BeEquivalentTo("fails", "holds", "unjudgeable");
        calc.Outcomes.Should().HaveSameCount(system.Relationships);
    }

    /// <remarks>
    /// Completeness is about values and says nothing about whether the checks passed. Folding the two together
    /// would leave a caller unable to ask "did everything resolve?" of a model that has a finding.
    /// </remarks>
    [Fact]
    public void ACalculationWithAViolationIsStillComplete()
    {
        var system = SystemOf(new DefinitelyLessThanOperator
        {
            Id = "under", Lhs = Bound("measured", 50), Rhs = Bound("limit", 10)
        });

        var calc = system.Calculate();

        calc.IsComplete.Should().BeTrue();
        calc.MissingValues.Should().BeEmpty();
        calc.Violations.Should().ContainSingle();
    }

    /// <remarks>
    /// The over-determined case meeting the calculation. A determining equality whose sides are both already
    /// known determines nothing — <c>FlatSystem.RedundantEquations</c> reports it as redundant — but it is
    /// still worth evaluating, because whether the redundant routes actually agree is the finding.
    /// </remarks>
    [Fact]
    public void ARedundantEquationIsStillCheckedAndAFailingOneIsReported()
    {
        var system = SystemOf(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Equation)
        {
            Id = "redundant", Lhs = Bound("a", 10), Rhs = Bound("b", 12)
        });

        system.Flatten().RedundantEquations.Should().ContainSingle();

        var calc = system.Calculate();

        calc.IsComplete.Should().BeTrue();
        calc.Inconsistencies.Select(o => o.Relationship.Id).Should().Equal("redundant");
    }

    /// <remarks>
    /// A derived operand is judged from the value the calculation already computed, so a relationship over
    /// composites is reported like any other — and its two sides come out of one walk, not two more.
    /// </remarks>
    [Fact]
    public void ARelationshipOverComputedExpressionsIsJudgedOnTheComputedValues()
    {
        var a = Bound("a", 3);
        var b = Bound("b", 4);
        var total = new SumExpression([a, b]) { Id = "total" };
        var limit = Bound("limit", 10);
        var system = SystemOf(new DefinitelyLessThanOperator { Id = "under", Lhs = total, Rhs = limit });

        var outcome = system.Calculate().Outcomes.Single();

        outcome.IsSatisfied.Should().BeTrue();
        outcome.Lhs!.KmsValue.Should().BeApproximately(7, 1e-9);
    }

    [Fact]
    public void OutcomeForFindsARelationshipAndAnswersNullForAStranger()
    {
        var measured = Bound("measured", 5);
        var limit = Bound("limit", 10);
        var mine = new DefinitelyLessThanOperator { Id = "mine", Lhs = measured, Rhs = limit };
        var stranger = new DefinitelyLessThanOperator { Id = "stranger", Lhs = measured, Rhs = limit };

        var calc = SystemOf(mine).Calculate();

        calc.OutcomeFor(mine)!.IsSatisfied.Should().BeTrue();
        calc.OutcomeFor(stranger).Should().BeNull();
    }

    /// <remarks>
    /// The role labelling, read off a real calculation. A requirement judges its <c>Lhs</c> against its
    /// <c>Rhs</c>; an equation judges neither side, so both views are null and the positional values are all
    /// there is.
    /// </remarks>
    [Fact]
    public void SubjectAndCriterionAreTheValuesOfTheSidesTheRelationshipDistinguishes()
    {
        var measured = Bound("measured", 50);
        var limit = Bound("limit", 10);
        var system = SystemOf(
            new DefinitelyLessThanOperator { Id = "requirement", Lhs = measured, Rhs = limit },
            new EqualityOperator(AgreementRule.Nominal, SolvingRole.Equation)
            {
                Id = "equation", Lhs = measured, Rhs = limit
            });

        var calc = system.Calculate();

        var requirement = calc.OutcomeFor(system.Relationships.First(r => r.Id == "requirement"))!;
        requirement.HasCriterion.Should().BeTrue();
        requirement.Subject!.KmsValue.Should().BeApproximately(50, 1e-9);
        requirement.Criterion!.KmsValue.Should().BeApproximately(10, 1e-9);

        var equation = calc.OutcomeFor(system.Relationships.First(r => r.Id == "equation"))!;
        equation.HasCriterion.Should().BeFalse();
        equation.Subject.Should().BeNull();
        equation.Criterion.Should().BeNull();
        equation.Lhs!.KmsValue.Should().BeApproximately(50, 1e-9);
        equation.Rhs!.KmsValue.Should().BeApproximately(10, 1e-9);
    }

    /// <remarks>
    /// The third way a verdict can be unknown, and the one the seam had to widen to carry. The operands both
    /// resolved — nothing is missing — but the comparison the relationship asks for has no answer, because two
    /// unbounded ceilings say nothing about which is lower. That must reach the report as <i>undetermined</i>
    /// and not as a violation: an engineer told a requirement failed goes looking for a design problem, when
    /// what is actually wrong is that the check could not be run.
    /// </remarks>
    [Fact]
    public void AVerdictWhoseComparisonHasNoAnswerIsUndeterminedRatherThanAViolation()
    {
        var system = SystemOf(new UpperBoundsLessThanOperator
        {
            Id = "ceilings", Lhs = NoCeiling("a", 5), Rhs = NoCeiling("b", 10),
        });

        var outcome = system.Calculate().Outcomes.Single();

        outcome.IsUndetermined.Should().BeTrue();
        outcome.IsViolation.Should().BeFalse();
        outcome.IsInconsistency.Should().BeFalse();
        outcome.Lhs.Should().NotBeNull("both operands resolved; it is the comparison that has no answer");
        outcome.Rhs.Should().NotBeNull();
    }

    /// <remarks>
    /// The snapshot property extended to verdicts. Re-running is how a newer answer is obtained; a stored
    /// calculation must not quietly start reporting a different one.
    /// </remarks>
    [Fact]
    public void OutcomesAreASnapshotAndDoNotFollowLaterAssignments()
    {
        var measured = Bound("measured", 5);
        var system = SystemOf(new DefinitelyLessThanOperator
        {
            Id = "under", Lhs = measured, Rhs = Bound("limit", 10)
        });

        var calc = system.Calculate();
        calc.Outcomes.Single().IsSatisfied.Should().BeTrue();

        measured.Value = new Quantity(50, Dimensionality.Length)
            .Measurand(SymmetricUncertainty.FromRelErr(0));

        calc.Outcomes.Single().IsSatisfied.Should().BeTrue();
        system.Calculate().Outcomes.Single().IsSatisfied.Should().BeFalse();
    }
}
