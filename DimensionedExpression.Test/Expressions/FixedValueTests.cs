using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.Expressions;

/* Spec A §4.2: constants, reference values and scalars. Their constructors are internal (S2-2), so every node
   here is built the way anything outside the assembly builds one, from a snapshot. */
public class FixedValueTests
{
    private static readonly Dimensionality _acceleration =
        Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

    private static Measurand Exact(double kms, Dimensionality? dimensionality = null) =>
        new Quantity(kms, dimensionality ?? Dimensionality.Dimensionless).WithoutUncertainty();

    private static Measurand Uncertain(double kms, Dimensionality? dimensionality = null) =>
        new Quantity(kms, dimensionality ?? Dimensionality.Dimensionless)
            .Measurand(SymmetricUncertainty.FromRelative(0.01));

    private static FixedExpressionSnapshot Snapshot(
        FixedExpressionType type,
        string? name,
        Measurand value,
        IProvenance? provenance = null) =>
        new(type, name ?? "unnamed", name, value.GetSnapshot(), provenance?.GetSnapshot());

    private static Constant Gravity() =>
        Constant.FromSnapshot(Snapshot(FixedExpressionType.Constant, "g", Exact(9.80665, _acceleration)));

    // ── What a fixed value is ────────────────────────────────────────────────

    [Fact]
    public void AConstantHasItsValueFromTheMomentItIsBuilt()
    {
        var g = Gravity();

        g.Name.Should().Be("g");
        g.Value.KmsValue.Should().Be(9.80665);
        g.Dimensionality.Should().Be(_acceleration);
        g.IsFullyDescribed.Should().BeTrue();
        g.IsDirectlyMutable.Should().BeFalse();
        g.Children.Should().BeEmpty();
        g.UnsetVariables().Should().BeEmpty();
        g.ComputeIfFullyDescribed()!.KmsValue.Should().Be(9.80665);
    }

    [Fact]
    public void AReferenceValueKeepsItsUncertaintyAndItsProvenance()
    {
        var cited = ProvenanceFactory.Reference("CODATA 2018", year: 2018);
        var bigG = ReferenceValue.FromSnapshot(
            Snapshot(FixedExpressionType.ReferenceValue, "G", Uncertain(6.6743e-11), cited));

        bigG.Name.Should().Be("G");
        bigG.Value.RelativeUncertainty.Should().BeApproximately(0.01, 1e-12);
        bigG.Provenance!.Summary().Should().Be(cited.Summary());
    }

    [Fact]
    public void AScalarHasNoNameAndNoProvenance()
    {
        var two = Scalar.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.Scalar, "two", null, Exact(2).GetSnapshot(), null));

        two.Name.Should().BeNull();
        two.Provenance.Should().BeNull();
        two.Value.KmsValue.Should().Be(2);
        two.Dimensionality.Should().Be(Dimensionality.Dimensionless);
    }

    /* The leaf interfaces of §4.2: fixed and settable leaves are siblings, and a variable is not a fixed value. */
    [Fact]
    public void FixedValuesAndVariablesAreSiblingLeaves()
    {
        IExpression g = Gravity();
        IExpression m = new Variable("m", Exact(2, Dimensionality.Mass));

        g.Should().BeAssignableTo<ILeafExpression>().And.BeAssignableTo<IFixedExpression>();
        g.Should().NotBeAssignableTo<IDirectExpression>();
        m.Should().BeAssignableTo<ILeafExpression>().And.BeAssignableTo<IDirectExpression>();
        m.Should().NotBeAssignableTo<IFixedExpression>();

        ((ILeafExpression)g).Value!.KmsValue.Should().Be(9.80665);
        ((ILeafExpression)m).Value!.KmsValue.Should().Be(2);
        ((ILeafExpression)new Variable("unset", Dimensionality.Mass)).Value.Should().BeNull();
    }

    /* S2-6. `known` is how a calculation hands a variable its override. A fixed value answers with its own
       value whatever is there, or a caller could change a constant by seeding the dictionary. */
    [Fact]
    public void AFixedValueIgnoresAValueSuppliedForIt()
    {
        var g = Gravity();
        var known = new Dictionary<IExpression, Measurand> { [g] = Exact(1, _acceleration) };

        g.ComputeFrom(known)!.KmsValue.Should().Be(9.80665);
    }

    [Fact]
    public void AFormulaOverFixedValuesComputes()
    {
        var two = Scalar.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.Scalar, "two", null, Exact(2).GetSnapshot(), null));
        var h = new Variable("h", Dimensionality.Length);
        var twoGh = new ProductExpression([two, Gravity(), h]);

        twoGh.UnsetVariables().Should().Equal(h);
        twoGh.ComputeIfFullyDescribed().Should().BeNull();

        h.Value = Exact(5, Dimensionality.Length);

        twoGh.ComputeIfFullyDescribed()!.KmsValue.Should().BeApproximately(98.0665, 1e-9);
    }

    // ── The rule for values ──────────────────────────────────────────────────

    [Fact]
    public void TheRuleAcceptsTheValueEachKindIsFor()
    {
        FixedValueRule.Problem(FixedExpressionType.Constant, Exact(9.8, _acceleration)).Should().BeNull();
        FixedValueRule.Problem(FixedExpressionType.ReferenceValue, Uncertain(6.67e-11, _acceleration)).Should().BeNull();
        FixedValueRule.Problem(FixedExpressionType.Scalar, Exact(2)).Should().BeNull();
    }

    [Fact]
    public void TheRuleSaysWhyAValueIsRefused()
    {
        FixedValueRule.Problem(FixedExpressionType.Constant, Uncertain(9.8)).Should().Contain("reference value");
        FixedValueRule.Problem(FixedExpressionType.ReferenceValue, Exact(9.8)).Should().Contain("constant");
        FixedValueRule.Problem(FixedExpressionType.Scalar, Uncertain(2)).Should().Contain("uncertainty");
        FixedValueRule.Problem(FixedExpressionType.Scalar, Exact(2, Dimensionality.Mass)).Should().Contain("dimensionless");
    }

    /* An asymmetric uncertainty with one bound at zero is still an uncertainty. */
    [Fact]
    public void AValueWithOneBoundOfUncertaintyIsNotExact()
    {
        var oneSided = new Quantity(5, Dimensionality.Mass).Measurand(
            Uncertainty.Absolute(new Quantity(0.1, Dimensionality.Mass), new Quantity(0, Dimensionality.Mass)));

        FixedValueRule.Problem(FixedExpressionType.Constant, oneSided).Should().NotBeNull();
        FixedValueRule.Problem(FixedExpressionType.ReferenceValue, oneSided).Should().BeNull();
    }

    /* §4.2 item 1: the wrong kind is refused, never switched silently. The message is the rule's. */
    [Fact]
    public void AConstantWithUncertaintyIsRefused()
    {
        var value = Uncertain(9.8, _acceleration);
        var act = () => Constant.FromSnapshot(Snapshot(FixedExpressionType.Constant, "g", value));

        act.Should().Throw<InvalidFixedValueException>()
            .WithMessage(FixedValueRule.Problem(FixedExpressionType.Constant, value));
    }

    [Fact]
    public void AReferenceValueWithNoUncertaintyIsRefused()
    {
        var act = () => ReferenceValue.FromSnapshot(
            Snapshot(FixedExpressionType.ReferenceValue, "G", Exact(6.67e-11)));

        act.Should().Throw<InvalidFixedValueException>();
    }

    [Fact]
    public void AScalarWithADimensionOrAnUncertaintyIsRefused()
    {
        var dimensioned = () => Scalar.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.Scalar, "s", null, Exact(2, Dimensionality.Mass).GetSnapshot(), null));
        var uncertain = () => Scalar.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.Scalar, "s", null, Uncertain(2).GetSnapshot(), null));

        dimensioned.Should().Throw<InvalidFixedValueException>();
        uncertain.Should().Throw<InvalidFixedValueException>();
    }

    // ── Names ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(FixedExpressionType.Constant)]
    [InlineData(FixedExpressionType.ReferenceValue)]
    public void ANamedFixedValueMustHaveAValidName(FixedExpressionType type)
    {
        var value = type is FixedExpressionType.Constant ? Exact(1) : Uncertain(1);

        var unnamed = () => ExpressionFactory.FromSnapshot(
            new FixedExpressionSnapshot(type, "id", null, value.GetSnapshot(), null));
        var badlyNamed = () => ExpressionFactory.FromSnapshot(
            new FixedExpressionSnapshot(type, "id", " g", value.GetSnapshot(), null));

        unnamed.Should().Throw<InvalidNameException>();
        badlyNamed.Should().Throw<InvalidNameException>();
    }

    /* A scalar's snapshot with a name or a provenance is refused, not rebuilt with them dropped. */
    [Fact]
    public void AScalarSnapshotWithANameOrAProvenanceIsRefused()
    {
        var named = () => Scalar.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.Scalar, "s", "two", Exact(2).GetSnapshot(), null));
        var sourced = () => Scalar.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.Scalar, "s", null, Exact(2).GetSnapshot(),
            ProvenanceFactory.Reference("a table").GetSnapshot()));

        named.Should().Throw<ArgumentException>();
        sourced.Should().Throw<ArgumentException>();
    }

    // ── Snapshots ────────────────────────────────────────────────────────────

    [Fact]
    public void EachKindRebuildsFromItsOwnSnapshotThroughTheFactory()
    {
        var cited = ProvenanceFactory.Reference("CGPM 1901", year: 1901);
        var constant = Snapshot(FixedExpressionType.Constant, "g", Exact(9.80665, _acceleration), cited);
        var reference = Snapshot(FixedExpressionType.ReferenceValue, "G", Uncertain(6.67e-11));
        var scalar = new FixedExpressionSnapshot(FixedExpressionType.Scalar, "two", null, Exact(2).GetSnapshot(), null);

        var g = ExpressionFactory.FromSnapshot(constant).Should().BeOfType<Constant>().Which;
        var bigG = ExpressionFactory.FromSnapshot(reference).Should().BeOfType<ReferenceValue>().Which;
        var two = ExpressionFactory.FromSnapshot(scalar).Should().BeOfType<Scalar>().Which;

        g.GetSnapshot().Should().Be(constant);
        bigG.GetSnapshot().Should().Be(reference);
        two.GetSnapshot().Should().Be(scalar);
        g.Id.Should().Be("g");
    }

    [Fact]
    public void ASnapshotOfAnotherKindIsRefused()
    {
        var scalar = new FixedExpressionSnapshot(FixedExpressionType.Scalar, "two", null, Exact(2).GetSnapshot(), null);

        var act = () => Constant.FromSnapshot(scalar);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AFixedValuesNameIsStoredNormalized()
    {
        var decomposed = "é";
        var constant = Constant.FromSnapshot(Snapshot(FixedExpressionType.Constant, decomposed, Exact(1)));

        constant.Name.Should().Be("é");
    }
}
