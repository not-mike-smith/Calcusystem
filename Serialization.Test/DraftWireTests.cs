using System.Text.Json;
using System.Text.Json.Serialization;
using Calcusystem.Authoring.Drafts;
using Calcusystem.Authoring.Enums;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Comparison;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Units;
using Calcusystem.Serialization.Mappers;
using FluentAssertions;

namespace Calcusystem.Serialization.Test;

/* The draft wire shape, in both directions. A draft is freely invalid, so these cover the half-built states as
   deliberately as the complete ones — an untyped node and a reference to nothing have to survive a save. */
public class DraftWireTests
{
    /* The options the HTTP surface uses. Enums go over the wire as names, so adding a member cannot renumber an
       existing payload's meaning. */
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static SystemDraft Furnished()
    {
        var draft = SystemDraft.Create("Newton's second law", "F = m·a", "sys-1");

        var m = draft.AddVariable("m", Dimensionality.Mass, "var-m");
        m.Value = Mass.Kilogram.Quantity(2).WithUncertainty(1.0.Percent());
        m.Provenance = ProvenanceFactory.Measured("scale-7", new DateOnly(2026, 3, 1));
        m.ValueUnit = Mass.Pound.Symbol;

        var a = draft.AddVariable("a", Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time), "var-a");
        a.Provenance = ProvenanceFactory.Design("spec-12");

        var f = draft.AddExpression(ExpressionDraftType.Product, "expr-f");
        f.OperandIds.Add(m.Id);
        f.OperandIds.Add(a.Id);
        f.UncertaintyCorrelation = UncertaintyCorrelation.Correlated;

        var limit = draft.AddVariable("F-max", Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time), "var-max");
        limit.Value = Force.Newton.Quantity(100).WithUncertainty(0.5.Percent());

        var check = draft.AddRelationship(BinaryOperatorType.SimpleComparison, "rel-1");
        check.LhsId = f.Id;
        check.RhsId = limit.Id;
        check.SolvingRole = SolvingRole.Requirement;
        check.Rule = new ComparisonRule(Landmark.UpperBound, MustBe.LessThan, Landmark.Nominal);
        check.Name = "under the rating";
        check.Description = "the force must stay below the rated maximum";
        check.Provenance = ProvenanceFactory.Reference("ASME B1.1", "https://example.org", 2019);

        return draft;
    }

    private static SystemDraft ThroughJson(SystemDraft draft)
    {
        var json = JsonSerializer.Serialize(DraftMapper.ToWire(draft), Options);
        return DraftMapper.FromWire(JsonSerializer.Deserialize<Dtos.SystemDraft>(json, Options)!);
    }

    [Fact]
    public void ADraftSurvivesTheWireWithItsIdentityIntact()
    {
        var restored = ThroughJson(Furnished());

        restored.Id.Should().Be("sys-1");
        restored.Name.Should().Be("Newton's second law");
        restored.Description.Should().Be("F = m·a");
        restored.Expressions.Select(e => e.Id).Should().BeEquivalentTo(["var-m", "var-a", "expr-f", "var-max"]);
        restored.Relationships.Select(r => r.Id).Should().BeEquivalentTo(["rel-1"]);
    }

    [Fact]
    public void AVariableSurvivesWithItsSymbolDimensionalityValueAndUncertainty()
    {
        var original = Furnished().Expression("var-m")!;
        var restored = ThroughJson(Furnished()).Expression("var-m")!;

        restored.Type.Should().Be(ExpressionDraftType.Variable);
        restored.Symbol.Should().Be("m");
        restored.Dimensionality.Should().Be(Dimensionality.Mass);
        restored.Value!.KmsValue.Should().Be(original.Value!.KmsValue);
        restored.Value!.Dimensionality.Should().Be(Dimensionality.Mass);
        restored.Value!.Uncertainty.GetSnapshot().Should().Be(original.Value!.Uncertainty.GetSnapshot());
        restored.ValueUnit.Should().Be(Mass.Pound.Symbol);
    }

    /* A reading preference, so it survives the wire but changes nothing about the model: the quantity is the
       KMS value either way, and a symbol naming no unit is a reading an editor cannot honor rather than a
       reason the draft is wrong. */
    [Fact]
    public void AUnitTheValueWasWrittenInIsCarriedButDecidesNothing()
    {
        var draft = SystemDraft.Create("written in pounds");
        var m = draft.AddVariable("m", Dimensionality.Mass, "var-m");
        m.Value = Mass.Kilogram.Quantity(2).WithUncertainty(1.0.Percent());
        m.ValueUnit = "not-a-unit";

        var restored = ThroughJson(draft);

        restored.Expression("var-m")!.ValueUnit.Should().Be("not-a-unit");
        restored.Expression("var-m")!.Value!.KmsValue.Should().Be(2);
        restored.Validate().Should().BeEmpty();
    }

    /* The stored form, not just the number: an uncertainty written as a fraction and read back as an absolute
       would keep its magnitude and change its meaning at every value but one. */
    [Fact]
    public void AnUncertaintyKeepsTheFormItWasStoredIn()
    {
        var wire = DraftMapper.ToWire(Furnished());

        wire.Expressions.Single(e => e.Id == "var-m").Uncertainty!.IsStoredAsAbs.Should().BeFalse();
        ThroughJson(Furnished()).Expression("var-m")!.Value!.RelativeUncertainty.Should().Be(0.01);
    }

    [Fact]
    public void AnOperandListKeepsItsOrderAndItsCorrelation()
    {
        var restored = ThroughJson(Furnished()).Expression("expr-f")!;

        restored.OperandIds.Should().Equal(["var-m", "var-a"]);
        restored.UncertaintyCorrelation.Should().Be(UncertaintyCorrelation.Correlated);
    }

    [Fact]
    public void ARelationshipSurvivesWithItsRoleRuleAndText()
    {
        var restored = ThroughJson(Furnished()).Relationship("rel-1")!;

        restored.Type.Should().Be(BinaryOperatorType.SimpleComparison);
        restored.LhsId.Should().Be("expr-f");
        restored.RhsId.Should().Be("var-max");
        restored.SolvingRole.Should().Be(SolvingRole.Requirement);
        restored.Rule.Should().Be(new ComparisonRule(Landmark.UpperBound, MustBe.LessThan, Landmark.Nominal));
        restored.Name.Should().Be("under the rating");
        restored.Description.Should().Be("the force must stay below the rated maximum");
    }

    [Fact]
    public void ProvenanceSurvivesForEveryKindThatWasSupplied()
    {
        var restored = ThroughJson(Furnished());

        restored.Expression("var-m")!.Provenance!.GetSnapshot().Should()
            .Be(ProvenanceSnapshot.Measured(
                restored.Expression("var-m")!.Provenance!.Id, "scale-7", new DateOnly(2026, 3, 1)));
        restored.Expression("var-a")!.Provenance.Should().BeOfType<DesignProvenance>();
        restored.Relationship("rel-1")!.Provenance!.GetSnapshot().Citation.Should().Be("ASME B1.1");
    }

    /* The whole reason for a separate wire shape: every one of these is a state an editor passes through, and
       none of them may be lost or mended on the way to the server. */
    [Fact]
    public void AHalfBuiltDraftSurvivesExactlyAsItWas()
    {
        var draft = SystemDraft.Create("mid-edit");
        var untyped = draft.AddExpression(id: "nothing-yet");
        var sum = draft.AddExpression(ExpressionDraftType.Sum, "sum-1");
        sum.OperandIds.Add("nowhere");
        sum.OperandIds.Add(null);
        var dimensionless = draft.AddVariable("x", id: "var-x");
        var orphan = draft.AddRelationship(id: "rel-blank");

        var restored = ThroughJson(draft);

        restored.Expression(untyped.Id)!.Type.Should().BeNull();
        restored.Expression(sum.Id)!.OperandIds.Should().Equal(["nowhere", null]);
        restored.Expression(dimensionless.Id)!.Dimensionality.Should().BeNull();
        restored.Relationship(orphan.Id)!.Type.Should().BeNull();
        restored.Relationship(orphan.Id)!.LhsId.Should().BeNull();
    }

    /* Reachable in an editor by setting a value and then correcting the dimensionality. Writing only the
       declared one would mend the draft in transit, so a draft that failed to build would come back building. */
    [Fact]
    public void AValueThatDisagreesWithItsNodesDimensionalityStillDisagreesAfterTheWire()
    {
        var draft = SystemDraft.Create("mistaken");
        var wrong = draft.AddVariable("L", Dimensionality.Length, "var-L");
        wrong.Value = Mass.Kilogram.Quantity(3).WithUncertainty(0.0.Percent());

        var restored = ThroughJson(draft);

        restored.Expression("var-L")!.Dimensionality.Should().Be(Dimensionality.Length);
        restored.Expression("var-L")!.Value!.Dimensionality.Should().Be(Dimensionality.Mass);
        restored.Validate().Should().HaveSameCount(draft.Validate());
    }

    [Fact]
    public void ARoundTrippedDraftBuildsTheSameSystem()
    {
        var draft = Furnished();

        var original = draft.Build();
        var restored = ThroughJson(draft).Build();

        original.DidFail.Should().BeFalse();
        restored.DidFail.Should().BeFalse();
        restored.Match(s => s.Variables.Select(v => v.Id), _ => [])
            .Should().BeEquivalentTo(original.Match(s => s.Variables.Select(v => v.Id), _ => []));
    }

    /* A draft keys its nodes by id, so a repeated one would displace the node already held and the draft would
       come back missing a node nobody deleted. */
    [Fact]
    public void TwoNodesInOneListClaimingOneIdAreRefused()
    {
        var wire = new Dtos.SystemDraft
        {
            Expressions =
            [
                new Dtos.ExpressionDraft { Id = "same", Type = ExpressionDraftType.Variable },
                new Dtos.ExpressionDraft { Id = "same", Type = ExpressionDraftType.Sum },
            ],
        };

        var refusing = () => DraftMapper.FromWire(wire);

        refusing.Should().Throw<FormatException>().WithMessage("*same*");
    }

    /* An id shared across the two lists is a different matter: the draft holds it, and reports it. Refusing
       here would take that answer away from the editor. */
    [Fact]
    public void AnIdSharedByAnExpressionAndARelationshipIsHeldAndReported()
    {
        var wire = new Dtos.SystemDraft
        {
            Expressions = [new Dtos.ExpressionDraft { Id = "shared", Type = ExpressionDraftType.Variable, Dimensionality = "M1" }],
            Relationships = [new Dtos.RelationshipDraft { Id = "shared", Type = BinaryOperatorType.Equality }],
        };

        var draft = DraftMapper.FromWire(wire);

        draft.Validate().Should().Contain(p => p.Kind == DraftProblemKind.DuplicateId);
    }

    [Fact]
    public void AMalformedDimensionalityIsRefusedRatherThanReadAsDimensionless()
    {
        var wire = new Dtos.SystemDraft
        {
            Expressions = [new Dtos.ExpressionDraft { Id = "var-1", Dimensionality = "Z9" }],
        };

        var refusing = () => DraftMapper.FromWire(wire);

        refusing.Should().Throw<FormatException>();
    }

    /* An empty draft is a legitimate document — a model nobody has started. It has to survive its own absence
       of content, including the lists being left out of the payload entirely. */
    [Fact]
    public void AnEmptyPayloadReadsAsAnEmptyDraft()
    {
        var draft = DraftMapper.FromWire(JsonSerializer.Deserialize<Dtos.SystemDraft>("{}", Options)!);

        draft.Name.Should().BeEmpty();
        draft.Expressions.Should().BeEmpty();
        draft.Relationships.Should().BeEmpty();
        draft.Id.Should().NotBeNullOrEmpty();
    }
}
