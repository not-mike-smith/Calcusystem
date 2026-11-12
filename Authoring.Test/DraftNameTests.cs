using Calcusystem.Authoring.Drafts;
using Calcusystem.Authoring.Enums;
using Calcusystem.Core.Either;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Authoring.Test;

/* Spec A §4.1, on drafts: who must have a name, what a valid one is, that names are unique across expressions
   and relationships, and that a node is found by its name. */
public class DraftNameTests
{
    private static ExpressionSystem Built(SystemDraft draft) =>
        draft.Build().Should().BeOfType<Succeeded<ExpressionSystem>>().Which.Value;

    private static RelationshipDraft Check(SystemDraft draft, ExpressionDraft lhs, ExpressionDraft rhs, string? name)
    {
        var check = draft.AddRelationship(BinaryOperatorType.DefinitelyLessThan);
        check.LhsId = lhs.Id;
        check.RhsId = rhs.Id;
        check.Name = name;
        return check;
    }

    // ── Who must have a name ─────────────────────────────────────────────────

    /* AU-20, as Spec A changes it: the draft no longer falls back to the id. */
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AVariableWithNoNameIsReported(string? name)
    {
        var draft = SystemDraft.Create("names");
        var m = draft.AddVariable(name, Dimensionality.Mass);

        draft.Validate().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Kind = DraftProblemKind.NameMissing, NodeId = m.Id });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AComputedNodeOrARelationshipNeedsNoName(string? name)
    {
        var draft = SystemDraft.Create("names");
        var m = draft.AddVariable("m", Dimensionality.Mass);
        var negated = draft.AddExpression(ExpressionDraftType.Negated);
        negated.OperandIds.Add(m.Id);
        negated.Name = name;
        Check(draft, negated, m, name);

        var system = Built(draft);

        system.DerivedExpressions.Single().Name.Should().BeNull();
        system.Relationships.Single().Name.Should().BeNull();
    }

    [Fact]
    public void AComputedNodesNameCarriesThroughToTheBuiltNode()
    {
        var draft = SystemDraft.Create("names");
        var m = draft.AddVariable("m", Dimensionality.Mass);

        foreach (var kind in new[]
                 {
                     ExpressionDraftType.Sum, ExpressionDraftType.Product, ExpressionDraftType.Negated,
                     ExpressionDraftType.Reciprocal,
                 })
        {
            var node = draft.AddExpression(kind);
            node.OperandIds.Add(m.Id);
            node.Name = $"a {kind}";
        }

        var quotient = draft.AddExpression(ExpressionDraftType.Quotient);
        quotient.OperandIds.Add(m.Id);
        quotient.OperandIds.Add(m.Id);
        quotient.Name = "ratio";

        foreach (var kind in new[]
                 {
                     ExpressionDraftType.Sqrt, ExpressionDraftType.Exponential, ExpressionDraftType.NaturalLog,
                 })
        {
            var node = draft.AddExpression(kind);
            node.OperandIds.Add(quotient.Id);
            node.Name = $"a {kind}";
        }

        var system = Built(draft);

        system.DerivedExpressions.Select(e => e.Name).Should().BeEquivalentTo(
            "a Sum", "a Product", "a Negated", "a Reciprocal", "ratio", "a Sqrt", "a Exponential", "a NaturalLog");
        system.Find("ratio")!.Id.Should().Be(quotient.Id);
    }

    // ── Valid names ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(" ")]
    [InlineData(" m")]
    [InlineData("m ")]
    [InlineData("a​b")]
    public void AnInvalidNameIsReportedOnWhicheverNodeHasIt(string name)
    {
        var draft = SystemDraft.Create("names");
        var variable = draft.AddVariable(name, Dimensionality.Mass);
        var ok = draft.AddVariable("ok", Dimensionality.Mass);
        var computed = draft.AddExpression(ExpressionDraftType.Negated);
        computed.OperandIds.Add(ok.Id);
        computed.Name = name;
        var relationship = Check(draft, ok, ok, name);

        var problems = draft.Validate();

        problems.Where(p => p.Kind is DraftProblemKind.NameInvalid).Select(p => p.NodeId)
            .Should().BeEquivalentTo([variable.Id, computed.Id, relationship.Id]);
        problems.Should().OnlyContain(
            p => p.Kind == DraftProblemKind.NameInvalid, "an invalid name is not also a duplicate");
    }

    /* Not a case of the theory above: an attribute argument is stored as UTF-8, which cannot carry a lone
       surrogate, so the test would be handed replacement characters and a valid name. */
    [Fact]
    public void ANameThatIsNotValidUnicodeIsReportedAsInvalid()
    {
        var draft = SystemDraft.Create("names");
        var m = draft.AddVariable("a\uD800b", Dimensionality.Mass);

        draft.Validate().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Kind = DraftProblemKind.NameInvalid, NodeId = m.Id });
    }

    /* AU-5: nothing is checked when it is set, and no member throws because a model is wrong. A lone surrogate
       cannot be normalized, so it is the case most likely to throw. */
    [Fact]
    public void AnyNameCanBeHeldWithoutThrowing()
    {
        var draft = SystemDraft.Create("names");
        var node = draft.AddVariable("a\uD800b", Dimensionality.Mass);

        node.Name.Should().Be("a\uD800b");
        node.Name = "  ";
        node.Name = null;
        draft.ByName("a\uD800b").Should().BeNull();
    }

    /* Spec A decision S1-4. */
    [Fact]
    public void ADraftHoldsANameNormalized()
    {
        var draft = SystemDraft.Create("names");
        var e = draft.AddVariable("é", Dimensionality.Mass);

        e.Name.Should().Be("é");
        Built(draft).Variables.Single().Name.Should().Be("é");
    }

    /* A node above one with a bad name is a consequence, not a cause (AU-35). */
    [Fact]
    public void NodesAboveABadlyNamedOneStaySilent()
    {
        var draft = SystemDraft.Create("names");
        var unnamed = draft.AddVariable(null, Dimensionality.Mass);
        var above = draft.AddExpression(ExpressionDraftType.Negated);
        above.OperandIds.Add(unnamed.Id);
        Check(draft, above, unnamed, "check");

        draft.Validate().Should().ContainSingle().Which.NodeId.Should().Be(unnamed.Id);
    }

    // ── Unique names ─────────────────────────────────────────────────────────

    [Fact]
    public void ADuplicateNameIsReportedOnceForEachNodeInvolved()
    {
        var draft = SystemDraft.Create("names");
        var first = draft.AddVariable("m", Dimensionality.Mass);
        var second = draft.AddVariable("m", Dimensionality.Mass);
        var third = draft.AddExpression(ExpressionDraftType.Negated);
        third.OperandIds.Add(first.Id);
        third.Name = "m";
        draft.AddVariable("other", Dimensionality.Mass);

        var problems = draft.Validate();

        problems.Should().OnlyContain(p => p.Kind == DraftProblemKind.DuplicateName);
        problems.Select(p => p.NodeId).Should().BeEquivalentTo([first.Id, second.Id, third.Id]);
    }

    /* One namespace across expressions and relationships. */
    [Fact]
    public void ARelationshipSharingAnExpressionsNameIsADuplicate()
    {
        var draft = SystemDraft.Create("names");
        var m = draft.AddVariable("m", Dimensionality.Mass);
        var limit = draft.AddVariable("limit", Dimensionality.Mass);
        var check = Check(draft, m, limit, "limit");

        draft.Validate().Should().OnlyContain(p => p.Kind == DraftProblemKind.DuplicateName)
            .And.Subject.Select(p => p.NodeId).Should().BeEquivalentTo([limit.Id, check.Id]);
    }

    [Fact]
    public void NamesThatNormalizeAlikeAreDuplicates()
    {
        var draft = SystemDraft.Create("names");
        draft.AddVariable("é", Dimensionality.Mass);
        draft.AddVariable("é", Dimensionality.Mass);

        draft.Validate().Should().HaveCount(2)
            .And.OnlyContain(p => p.Kind == DraftProblemKind.DuplicateName);
    }

    [Fact]
    public void NamesDifferingOnlyByCaseAreNotDuplicates()
    {
        var draft = SystemDraft.Create("names");
        draft.AddVariable("Re", Dimensionality.Mass);
        draft.AddVariable("re", Dimensionality.Mass);

        draft.Validate().Should().BeEmpty();
    }

    [Fact]
    public void UnnamedNodesAreNotDuplicatesOfEachOther()
    {
        var draft = SystemDraft.Create("names");
        var m = draft.AddVariable("m", Dimensionality.Mass);
        for (var i = 0; i < 2; i++)
        {
            var negated = draft.AddExpression(ExpressionDraftType.Negated);
            negated.OperandIds.Add(m.Id);
            negated.Name = i == 0 ? null : "";
            Check(draft, negated, m, null);
        }

        draft.Validate().Should().BeEmpty();
    }

    /* A reused name leaves operand references unambiguous, unlike a reused id, so the nodes above still report
       what is wrong with them in the same build. */
    [Fact]
    public void ADuplicateNameDoesNotHideOtherProblems()
    {
        var draft = SystemDraft.Create("names");
        var m = draft.AddVariable("m", Dimensionality.Mass);
        var length = draft.AddVariable("m", Dimensionality.Length);
        var sum = draft.AddExpression(ExpressionDraftType.Sum);
        sum.OperandIds.Add(m.Id);
        sum.OperandIds.Add(length.Id);

        var problems = draft.Validate();

        problems.Should().Contain(p => p.Kind == DraftProblemKind.DomainRule && p.NodeId == sum.Id);
        problems.Count(p => p.Kind == DraftProblemKind.DuplicateName).Should().Be(2);
    }

    /* AU-16 still holds with the name checks in place. */
    [Fact]
    public void ADraftWithADuplicateNameDoesNotBuild()
    {
        var draft = SystemDraft.Create("names");
        draft.AddVariable("m", Dimensionality.Mass);
        draft.AddVariable("m", Dimensionality.Mass);

        draft.Build().Should().BeOfType<Failed<ExpressionSystem>>();
    }

    // ── Finding by name ──────────────────────────────────────────────────────

    [Fact]
    public void ANodeIsFoundByItsNameInADraft()
    {
        var draft = SystemDraft.Create("names");
        var m = draft.AddVariable("m", Dimensionality.Mass);
        var limit = draft.AddVariable("é", Dimensionality.Mass);
        var check = Check(draft, m, limit, "check");

        draft.ByName("m").Should().BeSameAs(m);
        draft.ByName("é").Should().BeSameAs(limit, "lookup compares after normalization");
        draft.ByName("check").Should().BeNull("a relationship is not an expression");
        draft.ByName("absent").Should().BeNull();
        draft.RelationshipByName("check").Should().BeSameAs(check);
        draft.RelationshipByName("m").Should().BeNull();
    }

    /* As Expression(id) does for a reused id (AU-7). */
    [Fact]
    public void WhereANameIsReusedTheFirstNodeAddedIsFound()
    {
        var draft = SystemDraft.Create("names");
        var first = draft.AddVariable("m", Dimensionality.Mass);
        draft.AddVariable("m", Dimensionality.Mass);

        draft.ByName("m").Should().BeSameAs(first);
    }

    [Fact]
    public void AnUnnamedNodeIsNotFoundByAnEmptyName()
    {
        var draft = SystemDraft.Create("names");
        draft.AddVariable("", Dimensionality.Mass);
        draft.AddVariable(null, Dimensionality.Mass);

        draft.ByName("").Should().BeNull();
    }
}
