using Calcusystem.Authoring.Drafts;
using Calcusystem.Authoring.Enums;
using Calcusystem.Core.Either;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Authoring.Test;

/* Spec A §4.2, on drafts: constants, reference values and scalars. AU-47 to AU-52. */
public class DraftFixedValueTests
{
    private static readonly Dimensionality _acceleration =
        Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

    private static Measurand Exact(double kms, Dimensionality? dimensionality = null) =>
        new Quantity(kms, dimensionality ?? Dimensionality.Dimensionless).WithoutUncertainty();

    private static Measurand Uncertain(double kms, Dimensionality? dimensionality = null) =>
        new Quantity(kms, dimensionality ?? Dimensionality.Dimensionless)
            .Measurand(SymmetricUncertainty.FromRelative(0.01));

    private static ExpressionSystem Built(SystemDraft draft) =>
        draft.Build().Should().BeOfType<Succeeded<ExpressionSystem>>().Which.Value;

    // ── Building ─────────────────────────────────────────────────────────────

    /* AU-47, and Spec A acceptance 4. */
    [Fact]
    public void EachKindBuildsIntoItsOwnNodeAndNoneIsAVariable()
    {
        var draft = SystemDraft.Create("fixed values");
        var g = draft.AddConstant("g", Exact(9.80665, _acceleration));
        var bigG = draft.AddReferenceValue("G", Uncertain(6.6743e-11));
        var two = draft.AddScalar(2);

        var system = Built(draft);

        system.Variables.Should().BeEmpty();
        system.Givens.Select(given => given.Id).Should().BeEquivalentTo(g.Id, bigG.Id);
        system.Find("g").Should().BeOfType<Constant>().Which.Value.KmsValue.Should().Be(9.80665);
        system.Find("G").Should().BeOfType<ReferenceValue>();
        system.GetAllExpressions().Single(e => e.Id == two.Id).Should().BeOfType<Scalar>()
            .Which.Value.KmsValue.Should().Be(2);
    }

    [Fact]
    public void TheAddMethodsSetWhatTheKindReads()
    {
        var draft = SystemDraft.Create("adders");

        var g = draft.AddConstant("g", Exact(9.80665, _acceleration), "id-g");
        var bigG = draft.AddReferenceValue("G", Uncertain(6.6743e-11), "id-G");
        var two = draft.AddScalar(2, "id-two");
        var later = draft.AddScalar();

        g.Should().BeEquivalentTo(new
        {
            Id = "id-g", Type = ExpressionDraftType.Constant, Name = "g", Dimensionality = (Dimensionality?)_acceleration,
        });
        g.Value!.KmsValue.Should().Be(9.80665);
        bigG.Should().BeEquivalentTo(new
        {
            Id = "id-G", Type = ExpressionDraftType.ReferenceValue, Name = "G",
            Dimensionality = (Dimensionality?)Dimensionality.Dimensionless,
        });
        two.Type.Should().Be(ExpressionDraftType.Scalar);
        two.Id.Should().Be("id-two");
        two.Value!.KmsValue.Should().Be(2);
        two.Value.Dimensionality.Should().Be(Dimensionality.Dimensionless);
        two.Value.KmsAbsoluteUncertainty.Should().Be(0);
        later.Value.Should().BeNull();
        draft.AddConstant().Dimensionality.Should().BeNull();
    }

    [Fact]
    public void AFormulaOverFixedValuesBuilds()
    {
        var draft = SystemDraft.Create("2·g·h");
        var two = draft.AddScalar(2);
        var g = draft.AddConstant("g", Exact(9.80665, _acceleration));
        var h = draft.AddVariable("h", Dimensionality.Length);
        var product = draft.AddExpression(ExpressionDraftType.Product);
        product.OperandIds.AddRange([two.Id, g.Id, h.Id]);

        var system = Built(draft);

        system.DerivedExpressions.Single().Dimensionality.Should().Be(_acceleration * Dimensionality.Length);
        system.Variables.Should().ContainSingle().Which.Name.Should().Be("h");
    }

    [Fact]
    public void AGivensProvenanceCarriesThroughToTheBuiltNode()
    {
        var draft = SystemDraft.Create("provenance");
        var g = draft.AddConstant("g", Exact(9.80665, _acceleration));
        g.Provenance = ProvenanceFactory.Reference("CGPM 1901", year: 1901);

        Built(draft).Givens.Single().Provenance!.Summary().Should().Be("Reference: CGPM 1901 (1901)");
    }

    // ── What each kind must have ─────────────────────────────────────────────

    /* AU-42, widened: a constant and a reference value must have a name, as a variable must. */
    [Theory]
    [InlineData(ExpressionDraftType.Constant)]
    [InlineData(ExpressionDraftType.ReferenceValue)]
    public void AGivenWithNoNameIsReported(ExpressionDraftType type)
    {
        var draft = SystemDraft.Create("unnamed");
        var node = draft.AddExpression(type);
        node.Dimensionality = Dimensionality.Dimensionless;
        node.Value = type is ExpressionDraftType.Constant ? Exact(1) : Uncertain(1);

        draft.Validate().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Kind = DraftProblemKind.NameMissing, NodeId = node.Id });
    }

    /* AU-26, widened (S2-9): no dimensionless default for a given, as there is none for a variable. */
    [Fact]
    public void AGivenWithNoDimensionalityIsReported()
    {
        var draft = SystemDraft.Create("undimensioned");
        var node = draft.AddExpression(ExpressionDraftType.Constant);
        node.Name = "g";
        node.Value = Exact(9.8);

        draft.Validate().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Kind = DraftProblemKind.DimensionalityMissing, NodeId = node.Id });
    }

    /* AU-48. A variable may be left unset; a fixed value may not. */
    [Fact]
    public void AFixedValueWithNoValueIsReported()
    {
        var draft = SystemDraft.Create("valueless");
        var constant = draft.AddConstant("g");
        constant.Dimensionality = _acceleration;
        var reference = draft.AddReferenceValue("G");
        reference.Dimensionality = Dimensionality.Dimensionless;
        var scalar = draft.AddScalar();

        draft.Validate().Should().BeEquivalentTo(new[]
        {
            new { Kind = DraftProblemKind.ValueMissing, NodeId = constant.Id },
            new { Kind = DraftProblemKind.ValueMissing, NodeId = reference.Id },
            new { Kind = DraftProblemKind.ValueMissing, NodeId = scalar.Id },
        });
    }

    /* AU-49, and Spec A §4.2 item 1: the wrong kind is a problem, never switched silently. The message is the
       domain's (S2-3). */
    [Fact]
    public void AValueTheKindDoesNotAcceptIsReportedAsTheDomainStatesIt()
    {
        var draft = SystemDraft.Create("wrong kinds");
        var constant = draft.AddConstant("g", Uncertain(9.8, _acceleration));
        var reference = draft.AddReferenceValue("G", Exact(6.67e-11));
        var uncertainScalar = draft.AddScalar();
        uncertainScalar.Value = Uncertain(2);
        var dimensionedScalar = draft.AddScalar();
        dimensionedScalar.Value = Exact(2, Dimensionality.Mass);

        draft.Validate().Should().BeEquivalentTo(new[]
        {
            new
            {
                Kind = DraftProblemKind.FixedValueInvalid, NodeId = constant.Id,
                Message = FixedValueRule.Problem(FixedExpressionType.Constant, constant.Value!),
            },
            new
            {
                Kind = DraftProblemKind.FixedValueInvalid, NodeId = reference.Id,
                Message = FixedValueRule.Problem(FixedExpressionType.ReferenceValue, reference.Value!),
            },
            new
            {
                Kind = DraftProblemKind.FixedValueInvalid, NodeId = uncertainScalar.Id,
                Message = FixedValueRule.Problem(FixedExpressionType.Scalar, uncertainScalar.Value!),
            },
            new
            {
                Kind = DraftProblemKind.FixedValueInvalid, NodeId = dimensionedScalar.Id,
                Message = FixedValueRule.Problem(FixedExpressionType.Scalar, dimensionedScalar.Value!),
            },
        });
    }

    /* S2-10. Zero ± 5 % has absolute bounds of zero and is still a reference value, not a constant. */
    [Fact]
    public void AReferenceValueOfZeroWithARelativeUncertaintyBuilds()
    {
        var draft = SystemDraft.Create("zero");
        draft.AddReferenceValue(
            "offset", new Quantity(0, Dimensionality.Length).Measurand(SymmetricUncertainty.FromRelative(0.05)));

        Built(draft).Givens.Single().Should().BeOfType<ReferenceValue>();
    }

    /* AU-39, for the question more than one layer asks: every kind either is a leaf or takes operands. */
    [Fact]
    public void TheLeafKindsAreTheVariableAndTheFixedValues()
    {
        Enum.GetValues<ExpressionDraftType>().Where(kind => kind.IsLeaf()).Should().BeEquivalentTo(new[]
        {
            ExpressionDraftType.Variable, ExpressionDraftType.Constant,
            ExpressionDraftType.ReferenceValue, ExpressionDraftType.Scalar,
        });
    }

    /* AU-49 (S2-9): an editor can declare one dimensionality and leave a value of another, as with a variable. */
    [Fact]
    public void AGivenWhoseValueDisagreesWithItsDeclaredDimensionalityIsReported()
    {
        var draft = SystemDraft.Create("disagreeing");
        var g = draft.AddConstant("g", Exact(9.8, _acceleration));
        g.Dimensionality = Dimensionality.Mass;

        draft.Validate().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Kind = DraftProblemKind.FixedValueInvalid, NodeId = g.Id });
    }

    /* AU-32: one problem per node, the first in the fixed order: name, dimensionality, value, the rule. */
    [Fact]
    public void AFixedValueReportsOnlyItsFirstProblem()
    {
        var draft = SystemDraft.Create("ordered");
        var nothing = draft.AddExpression(ExpressionDraftType.Constant);
        var named = draft.AddExpression(ExpressionDraftType.Constant);
        named.Name = "c";

        draft.Validate().Should().BeEquivalentTo(new[]
        {
            new { Kind = DraftProblemKind.NameMissing, NodeId = nothing.Id },
            new { Kind = DraftProblemKind.DimensionalityMissing, NodeId = named.Id },
        });
    }

    /* Spec A acceptance 2: several mistakes, one report, nothing thrown. */
    [Fact]
    public void SeveralMistakesComeBackFromOneBuild()
    {
        var draft = SystemDraft.Create("several");
        draft.AddConstant("g", Uncertain(9.8, _acceleration));
        draft.AddScalar();
        draft.AddVariable(null, Dimensionality.Mass);

        var build = () => draft.Build();

        build.Should().NotThrow();
        draft.Validate().Select(p => p.Kind).Should().BeEquivalentTo(new[]
        {
            DraftProblemKind.FixedValueInvalid, DraftProblemKind.ValueMissing, DraftProblemKind.NameMissing,
        });
    }

    /* AU-35: a formula over a fixed value that failed says nothing of its own. */
    [Fact]
    public void ANodeAboveAFailedFixedValueStaysSilent()
    {
        var draft = SystemDraft.Create("cascade");
        var scalar = draft.AddScalar();
        var m = draft.AddVariable("m", Dimensionality.Mass);
        var product = draft.AddExpression(ExpressionDraftType.Product);
        product.OperandIds.AddRange([scalar.Id, m.Id]);

        draft.Validate().Should().ContainSingle().Which.NodeId.Should().Be(scalar.Id);
    }

    // ── A scalar has no name (S2-4) ──────────────────────────────────────────

    /* AU-50. Flipping a named node to a scalar keeps the name in the draft (AU-9) and ignores it everywhere. */
    [Fact]
    public void AScalarsNameIsIgnored()
    {
        var draft = SystemDraft.Create("scalar names");
        var m = draft.AddVariable("two", Dimensionality.Mass);
        var scalar = draft.AddScalar(2);
        scalar.Name = "two";
        var badlyNamed = draft.AddScalar(3);
        badlyNamed.Name = "  ";

        draft.Validate().Should().BeEmpty("a scalar's name is neither a duplicate nor invalid");
        draft.ByName("two").Should().BeSameAs(m);

        var system = Built(draft);
        system.GetAllExpressions().OfType<Scalar>().Should().HaveCount(2).And.OnlyContain(s => s.Name == null);
        system.Find("two").Should().BeOfType<Variable>();
    }

    [Fact]
    public void AScalarIgnoresADeclaredDimensionalityAndAProvenance()
    {
        var draft = SystemDraft.Create("scalar leftovers");
        var scalar = draft.AddScalar(2);
        scalar.Dimensionality = Dimensionality.Mass;
        scalar.Provenance = ProvenanceFactory.Reference("left over");

        var built = Built(draft).GetAllExpressions().Single().Should().BeOfType<Scalar>().Which;

        built.Dimensionality.Should().Be(Dimensionality.Dimensionless);
        built.Provenance.Should().BeNull();
    }

    // ── Leaves ignore operands (AU-52) ───────────────────────────────────────

    /* A node flipped from a formula to a leaf keeps its operand list (AU-9). A leaf reads none of it, so a
       stale operand, even one naming nothing, is not a reason it cannot be built. */
    [Theory]
    [InlineData(ExpressionDraftType.Variable)]
    [InlineData(ExpressionDraftType.Constant)]
    [InlineData(ExpressionDraftType.Scalar)]
    public void ALeafIgnoresOperandsLeftOverFromAnotherKind(ExpressionDraftType type)
    {
        var draft = SystemDraft.Create("flipped");
        var node = draft.AddExpression(ExpressionDraftType.Product);
        node.OperandIds.AddRange(["gone", null]);
        node.Type = type;
        node.Name = "x";
        node.Dimensionality = Dimensionality.Dimensionless;
        node.Value = Exact(1);

        draft.Validate().Should().BeEmpty();
        Built(draft).GetAllExpressions().Should().ContainSingle().Which.Id.Should().Be(node.Id);
    }

    /* AU-19: ids carry through for the new kinds as for every other. */
    [Fact]
    public void AFixedValuesIdCarriesThroughToTheBuiltNode()
    {
        var draft = SystemDraft.Create("ids");
        draft.AddConstant("g", Exact(9.8, _acceleration), "const-g");
        draft.AddScalar(2, "scalar-2");

        Built(draft).GetAllExpressions().Select(e => e.Id).Should().BeEquivalentTo("const-g", "scalar-2");
    }

    /* AU-41: a given's name is in the one namespace. */
    [Fact]
    public void AGivenSharingAVariablesNameIsADuplicate()
    {
        var draft = SystemDraft.Create("shared name");
        var variable = draft.AddVariable("g", _acceleration);
        var constant = draft.AddConstant("g", Exact(9.8, _acceleration));

        draft.Validate().Should().BeEquivalentTo(new[]
        {
            new { Kind = DraftProblemKind.DuplicateName, NodeId = variable.Id },
            new { Kind = DraftProblemKind.DuplicateName, NodeId = constant.Id },
        });
    }
}
