using Calcusystem.Authoring.Drafts;
using Calcusystem.Authoring.Enums;
using Calcusystem.Core.Either;
using Calcusystem.Core.Enums;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Authoring.Test;

public class SystemDraftTests
{
    private static readonly Dimensionality _acceleration =
        Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

    private static Measurand Kilograms(double kms) =>
        new Quantity(kms, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelative(0.01));

    // ── Building ─────────────────────────────────────────────────────────────

    [Fact]
    public void ADraftOfNewtonsSecondLawBuilds()
    {
        var draft = SystemDraft.Create("Newton's second law", "F = m·a");

        var m = draft.AddVariable("m", Dimensionality.Mass);
        m.Value = Kilograms(2);
        var a = draft.AddVariable("a", _acceleration);

        var f = draft.AddExpression(ExpressionDraftType.Product);
        f.OperandIds.Add(m.Id);
        f.OperandIds.Add(a.Id);

        draft.Validate().Should().BeEmpty();
        var system = draft.Build().Should().BeOfType<Succeeded<ExpressionSystem>>().Which.Value;

        system.Name.Should().Be("Newton's second law");
        system.Variables.Select(v => v.Symbol).Should().BeEquivalentTo("m", "a");
        system.DerivedExpressions.Should().HaveCount(1);
    }

    /* A front-end holds ids, not objects, so an id it was given while the node was still half-built has to
       still name the same thing after the build. */
    [Fact]
    public void ANodesIdCarriesThroughToTheBuiltExpression()
    {
        var draft = SystemDraft.Create("ids", "");
        var m = draft.AddVariable("m", Dimensionality.Mass);
        var negated = draft.AddExpression(ExpressionDraftType.Negated);
        negated.OperandIds.Add(m.Id);

        var system = draft.Build().Should().BeOfType<Succeeded<ExpressionSystem>>().Which.Value;

        system.Variables.Single().Id.Should().Be(m.Id);
        system.DerivedExpressions.Single().Id.Should().Be(negated.Id);
        system.Id.Should().Be(draft.Id);
    }

    [Fact]
    public void ARelationshipBuildsAndKeepsItsSolvingRole()
    {
        var draft = SystemDraft.Create("relationship", "");
        var m = draft.AddVariable("m", Dimensionality.Mass);
        var spec = draft.AddVariable("spec", Dimensionality.Mass);
        spec.Value = Kilograms(5);

        var equation = draft.AddRelationship(BinaryOperatorType.Equality);
        equation.LhsId = m.Id;
        equation.RhsId = spec.Id;
        equation.Agreement = AgreementRule.Nominal;
        equation.SolvingRole = SolvingRole.Equation;

        var system = draft.Build().Should().BeOfType<Succeeded<ExpressionSystem>>().Which.Value;

        system.Relationships.Should().HaveCount(1);
        system.Equations.Should().HaveCount(1);
        system.Requirements.Should().BeEmpty();
    }

    /* Thirteen of the fourteen operators can only be requirements and offer no way to say otherwise, so leaving
       the role unset must give them the one they would have had. */
    [Fact]
    public void AnUnstatedSolvingRoleBuildsARequirement()
    {
        var draft = SystemDraft.Create("unstated role", "");
        var m = draft.AddVariable("m", Dimensionality.Mass);
        var limit = draft.AddVariable("limit", Dimensionality.Mass);

        var check = draft.AddRelationship(BinaryOperatorType.DefinitelyLessThan);
        check.LhsId = m.Id;
        check.RhsId = limit.Id;

        var system = draft.Build().Should().BeOfType<Succeeded<ExpressionSystem>>().Which.Value;
        system.Requirements.Should().HaveCount(1);
    }

    // ── Nothing throws while editing ─────────────────────────────────────────

    /* The whole reason a draft exists. Every one of these is a state an editor passes through, and every one of
       them throws if attempted on the domain types directly. */
    [Fact]
    public void AnyHalfFinishedStateCanBeHeldWithoutThrowing()
    {
        var draft = SystemDraft.Create("mid-edit", "");

        var untyped = draft.AddExpression();
        var noDimension = draft.AddVariable("x");
        var wrongValue = draft.AddVariable("y", Dimensionality.Length);
        wrongValue.Value = Kilograms(3);

        var sum = draft.AddExpression(ExpressionDraftType.Sum);
        sum.OperandIds.Add(null);
        sum.OperandIds.Add("not-a-real-id");

        var relationship = draft.AddRelationship();
        relationship.LhsId = untyped.Id;

        // Nothing above threw. Every complaint is data instead.
        var problems = draft.Validate();
        problems.Should().NotBeEmpty();
        problems.Select(p => p.NodeId).Should().Contain([untyped.Id, noDimension.Id, wrongValue.Id, sum.Id]);
    }

    [Fact]
    public void ValidateAndBuildNeverDisagree()
    {
        var draft = SystemDraft.Create("agreement", "");
        var broken = draft.AddExpression(ExpressionDraftType.Sqrt);

        draft.Validate().Should().NotBeEmpty();
        draft.Build().Should().BeOfType<Failed<ExpressionSystem>>();
        draft.Validate().Should().BeEquivalentTo(draft.Validate());

        broken.OperandIds.Add(draft.AddVariable("q", Dimensionality.Mass * Dimensionality.Mass).Id);

        draft.Validate().Should().BeEmpty();
        draft.Build().Should().BeOfType<Succeeded<ExpressionSystem>>();
    }

    /* The case that drove Failure<TDetail>: a failed build has many problems and an editor has to mark them
       all, so one message naming the first is useless. They ride up as the failure's detail. */
    [Fact]
    public void AFailedBuildCarriesEveryProblemAsTheFailuresDetail()
    {
        var draft = SystemDraft.Create("several problems", "");
        draft.AddExpression();                                   // no kind chosen
        draft.AddVariable("x");                                  // no dimensionality
        var sum = draft.AddExpression(ExpressionDraftType.Sum);  // mixes dimensionalities
        sum.OperandIds.Add(draft.AddVariable("m", Dimensionality.Mass).Id);
        sum.OperandIds.Add(draft.AddVariable("l", Dimensionality.Length).Id);

        var built = draft.Build();

        built.TryGetFailure(out var failure).Should().BeTrue();
        failure!.Reason.Should().Be(FailureReason.Unprocessable, "the draft is not ready, the request was fine");

        failure.Should().BeOfType<Failure<IReadOnlyList<DraftProblem>>>()
            .Which.Detail.Should().HaveCount(3).And.BeEquivalentTo(draft.Validate());
    }

    /* A caller that only needs to know whether it worked never sees the detail, which is the point of attaching
       it by subtyping rather than by a second type parameter on Either. */
    [Fact]
    public void ACallerThatOnlyAsksWhetherItBuiltNeedsNoneOfThat()
    {
        var draft = SystemDraft.Create("one problem", "");
        draft.AddExpression();

        draft.Build().DidFail.Should().BeTrue();
        draft.Build().Match(_ => "built", f => f.Message)
            .Should().Be("One problem stops this draft from building.");
    }

    // ── Shape problems ───────────────────────────────────────────────────────

    [Fact]
    public void ANodeWithNoKindChosenIsReported()
    {
        var draft = SystemDraft.Create("untyped", "");
        var node = draft.AddExpression();

        draft.Validate().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Kind = DraftProblemKind.TypeNotChosen, NodeId = node.Id });
    }

    [Fact]
    public void AVariableWithNoDimensionalityIsReported()
    {
        var draft = SystemDraft.Create("no dimensionality", "");
        var node = draft.AddVariable("x");

        draft.Validate().Should().ContainSingle()
            .Which.Kind.Should().Be(DraftProblemKind.DimensionalityMissing);
        draft.Validate().Single().NodeId.Should().Be(node.Id);
    }

    [Fact]
    public void AnEmptyOperandSlotIsReported()
    {
        var draft = SystemDraft.Create("empty slot", "");
        var product = draft.AddExpression(ExpressionDraftType.Product);
        product.OperandIds.Add(draft.AddVariable("m", Dimensionality.Mass).Id);
        product.OperandIds.Add(null);

        draft.Validate().Should().ContainSingle()
            .Which.Kind.Should().Be(DraftProblemKind.OperandMissing);
    }

    [Theory]
    [InlineData(ExpressionDraftType.Negated, 2)]
    [InlineData(ExpressionDraftType.Reciprocal, 0)]
    [InlineData(ExpressionDraftType.Quotient, 1)]
    [InlineData(ExpressionDraftType.Quotient, 3)]
    [InlineData(ExpressionDraftType.Sum, 0)]
    public void TheWrongNumberOfOperandsIsReported(ExpressionDraftType type, int operands)
    {
        var draft = SystemDraft.Create("wrong arity", "");
        var node = draft.AddExpression(type);
        for (var i = 0; i < operands; i++)
            node.OperandIds.Add(draft.AddVariable($"v{i}", Dimensionality.Mass).Id);

        draft.Validate().Should()
            .Contain(p => p.Kind == DraftProblemKind.OperandCountWrong && p.NodeId == node.Id);
    }

    [Fact]
    public void AnOperandNamingNothingIsReported()
    {
        var draft = SystemDraft.Create("dangling", "");
        var node = draft.AddExpression(ExpressionDraftType.Negated);
        node.OperandIds.Add("nothing-has-this-id");

        draft.Validate().Should().ContainSingle()
            .Which.Kind.Should().Be(DraftProblemKind.ReferenceNotFound);
    }

    /* Removal deliberately leaves references where they are: scrubbing them would silently discard structure the
       author built, and a dangling reference is the more useful thing to show. */
    [Fact]
    public void RemovingANodeLeavesReferencesToItDanglingAndReported()
    {
        var draft = SystemDraft.Create("removal", "");
        var m = draft.AddVariable("m", Dimensionality.Mass);
        var negated = draft.AddExpression(ExpressionDraftType.Negated);
        negated.OperandIds.Add(m.Id);

        draft.Validate().Should().BeEmpty();

        draft.Remove(m.Id).Should().BeTrue();

        negated.OperandIds.Should().Equal(m.Id);
        draft.Validate().Should().ContainSingle()
            .Which.Kind.Should().Be(DraftProblemKind.ReferenceNotFound);
    }

    [Fact]
    public void ACycleIsReportedRatherThanHanging()
    {
        var draft = SystemDraft.Create("cycle", "");
        var first = draft.AddExpression(ExpressionDraftType.Negated);
        var second = draft.AddExpression(ExpressionDraftType.Negated);
        first.OperandIds.Add(second.Id);
        second.OperandIds.Add(first.Id);

        var problems = draft.Validate();

        problems.Should().HaveCount(2);
        problems.Should().OnlyContain(p => p.Kind == DraftProblemKind.CircularReference);
    }

    // ── The domain's own rules, relayed ──────────────────────────────────────

    [Fact]
    public void ASumOfDisagreeingDimensionalitiesIsReportedAsTheDomainStatesIt()
    {
        var draft = SystemDraft.Create("bad sum", "");
        var sum = draft.AddExpression(ExpressionDraftType.Sum);
        sum.OperandIds.Add(draft.AddVariable("m", Dimensionality.Mass).Id);
        sum.OperandIds.Add(draft.AddVariable("l", Dimensionality.Length).Id);

        draft.Validate().Should().ContainSingle()
            .Which.Should().Match<DraftProblem>(p =>
                p.Kind == DraftProblemKind.DomainRule && p.NodeId == sum.Id);
    }

    [Fact]
    public void AnExponentialOfADimensionedArgumentIsReported()
    {
        var draft = SystemDraft.Create("bad exponential", "");
        var exp = draft.AddExpression(ExpressionDraftType.Exponential);
        exp.OperandIds.Add(draft.AddVariable("m", Dimensionality.Mass).Id);

        draft.Validate().Should().ContainSingle()
            .Which.Kind.Should().Be(DraftProblemKind.DomainRule);
    }

    /* Sqrt's rule is the argument's exponents, not its dimensionality, so it is worth a case of its own
       alongside the sum and the exponential. */
    [Fact]
    public void ASqrtOfAnOddExponentIsReported()
    {
        var draft = SystemDraft.Create("bad sqrt", "");
        var root = draft.AddExpression(ExpressionDraftType.Sqrt);
        root.OperandIds.Add(draft.AddVariable("m", Dimensionality.Mass).Id);

        draft.Validate().Should().ContainSingle()
            .Which.Should().Match<DraftProblem>(p =>
                p.Kind == DraftProblemKind.DomainRule && p.NodeId == root.Id);
    }

    [Fact]
    public void AValueDisagreeingWithItsVariablesDimensionalityIsReported()
    {
        var draft = SystemDraft.Create("bad value", "");
        var length = draft.AddVariable("l", Dimensionality.Length);
        length.Value = Kilograms(3);

        draft.Validate().Should().ContainSingle()
            .Which.Kind.Should().Be(DraftProblemKind.DomainRule);
    }

    [Fact]
    public void ARelationshipAcrossDimensionalitiesIsReported()
    {
        var draft = SystemDraft.Create("bad relationship", "");
        var mass = draft.AddVariable("m", Dimensionality.Mass);
        var length = draft.AddVariable("l", Dimensionality.Length);

        var check = draft.AddRelationship(BinaryOperatorType.DefinitelyLessThan);
        check.LhsId = mass.Id;
        check.RhsId = length.Id;

        draft.Validate().Should().ContainSingle()
            .Which.Should().Match<DraftProblem>(p =>
                p.Kind == DraftProblemKind.DomainRule && p.NodeId == check.Id);
    }

    [Fact]
    public void AnEqualityWithNoAgreementRuleIsReported()
    {
        var draft = SystemDraft.Create("no agreement", "");
        var equality = draft.AddRelationship(BinaryOperatorType.Equality);
        equality.LhsId = draft.AddVariable("a", Dimensionality.Mass).Id;
        equality.RhsId = draft.AddVariable("b", Dimensionality.Mass).Id;

        draft.Validate().Should().ContainSingle()
            .Which.Kind.Should().Be(DraftProblemKind.RelationshipRuleMissing);
    }

    [Fact]
    public void ASimpleComparisonWithNoRuleIsReported()
    {
        var draft = SystemDraft.Create("no rule", "");
        var comparison = draft.AddRelationship(BinaryOperatorType.SimpleComparison);
        comparison.LhsId = draft.AddVariable("a", Dimensionality.Mass).Id;
        comparison.RhsId = draft.AddVariable("b", Dimensionality.Mass).Id;

        draft.Validate().Should().ContainSingle()
            .Which.Kind.Should().Be(DraftProblemKind.RelationshipRuleMissing);
    }

    // ── One cause, one problem ───────────────────────────────────────────────

    /* A bad sum three levels down would otherwise report four times, and an author would have to work out which
       of the four to fix. Only the cause speaks. */
    [Fact]
    public void NodesAboveAFailedOneStaySilent()
    {
        var draft = SystemDraft.Create("cascade", "");
        var sum = draft.AddExpression(ExpressionDraftType.Sum);
        sum.OperandIds.Add(draft.AddVariable("m", Dimensionality.Mass).Id);
        sum.OperandIds.Add(draft.AddVariable("l", Dimensionality.Length).Id);

        var negated = draft.AddExpression(ExpressionDraftType.Negated);
        negated.OperandIds.Add(sum.Id);
        var reciprocal = draft.AddExpression(ExpressionDraftType.Reciprocal);
        reciprocal.OperandIds.Add(negated.Id);

        var relationship = draft.AddRelationship(BinaryOperatorType.DefinitelyLessThan);
        relationship.LhsId = reciprocal.Id;
        relationship.RhsId = draft.AddVariable("limit", Dimensionality.Mass).Id;

        draft.Validate().Should().ContainSingle()
            .Which.NodeId.Should().Be(sum.Id);
    }

    [Fact]
    public void AnEmptyDraftBuildsAnEmptySystem()
    {
        var draft = SystemDraft.Create("empty", "nothing in it yet");

        var system = draft.Build().Should().BeOfType<Succeeded<ExpressionSystem>>().Which.Value;

        system.Variables.Should().BeEmpty();
        system.Relationships.Should().BeEmpty();
    }
}
