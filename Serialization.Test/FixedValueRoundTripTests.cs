using System.Text.Json;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Uncertainties;
using Calcusystem.Serialization.Mappers;
using FluentAssertions;

namespace Calcusystem.Serialization.Test;

/* Spec A §4.2 item 5: the wire form of a constant, a reference value and a scalar in a built system. Pushed
   through a real serializer, for the reason JsonRoundTripTests gives. */
public class FixedValueRoundTripTests
{
    private static readonly Dimensionality _acceleration =
        Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

    private static Constant Gravity() =>
        Constant.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.Constant,
            "const-g",
            "g",
            new Quantity(9.80665, _acceleration).WithoutUncertainty().GetSnapshot(),
            ProvenanceFactory.Reference("CGPM 1901", year: 1901).GetSnapshot()));

    private static ReferenceValue BigG() =>
        ReferenceValue.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.ReferenceValue,
            "ref-G",
            "G",
            new Quantity(6.6743e-11, Dimensionality.Dimensionless)
                .Measurand(SymmetricUncertainty.FromRelative(2.2e-5)).GetSnapshot(),
            null));

    private static Scalar Two() =>
        Scalar.FromSnapshot(new FixedExpressionSnapshot(
            FixedExpressionType.Scalar,
            "scalar-2",
            null,
            new Quantity(2, Dimensionality.Dimensionless).WithoutUncertainty().GetSnapshot(),
            null));

    private static ExpressionSystem Furnished()
    {
        var system = ExpressionSystem.Create("fixed values", "");
        var h = new Variable("h", Dimensionality.Length, "var-h");
        system.Add(new ProductExpression([Two(), Gravity(), h]) { Id = "expr-2gh" });
        system.Add(BigG());
        return system;
    }

    private static Dtos.ExpressionSystem ThroughJson(Dtos.ExpressionSystem dto) =>
        JsonSerializer.Deserialize<Dtos.ExpressionSystem>(JsonSerializer.Serialize(dto))!;

    private static ExpressionSystem RoundTrip(ExpressionSystem system) =>
        new DeserializingMapper(new DeserializationContext()).Map(ThroughJson(new SerializingMapper().Map(system)));

    [Fact]
    public void GivensAndScalarsAreWrittenInListsOfTheirOwn()
    {
        var dto = new SerializingMapper().Map(Furnished());

        dto.Givens.Select(g => (g.Id, g.Type, g.Name)).Should().BeEquivalentTo(new[]
        {
            ("const-g", "Constant", (string?)"g"),
            ("ref-G", "ReferenceValue", (string?)"G"),
        });
        dto.Scalars.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Id = "scalar-2", Type = "Scalar", Name = (string?)null, KmsValue = 2d });
        dto.Variables.Should().ContainSingle().Which.Id.Should().Be("var-h");
    }

    [Fact]
    public void AConstantSurvivesWithItsValueDimensionalityAndProvenance()
    {
        var restored = RoundTrip(Furnished());

        var g = restored.Find("g").Should().BeOfType<Constant>().Which;
        g.Id.Should().Be("const-g");
        g.Value.KmsValue.Should().Be(9.80665);
        g.Value.KmsAbsoluteUncertainty.Should().Be(0);
        g.Dimensionality.Should().Be(_acceleration);
        g.Provenance!.Summary().Should().Be("Reference: CGPM 1901 (1901)");
    }

    [Fact]
    public void AReferenceValueSurvivesWithItsUncertainty()
    {
        var restored = RoundTrip(Furnished());

        var bigG = restored.Find("G").Should().BeOfType<ReferenceValue>().Which;
        bigG.Value.KmsValue.Should().Be(6.6743e-11);
        bigG.Value.RelativeUncertainty.Should().BeApproximately(2.2e-5, 1e-15);
        bigG.Provenance.Should().BeNull();
    }

    /* The sharing survives too: the product's first factor is the system's one scalar, not a copy. */
    [Fact]
    public void AScalarSurvivesAsTheSameNodeItsFormulaRefersTo()
    {
        var restored = RoundTrip(Furnished());

        var scalar = restored.GetAllExpressions().OfType<Scalar>().Should().ContainSingle().Which;
        scalar.Id.Should().Be("scalar-2");
        scalar.Value.KmsValue.Should().Be(2);
        restored.DerivedExpressions.Single().Children.First().Should().BeSameAs(scalar);
        restored.Givens.Should().HaveCount(2);
        restored.Variables.Should().ContainSingle();
    }

    [Fact]
    public void AFixedValueOfAnUnknownTypeIsRefused()
    {
        var dto = ThroughJson(new SerializingMapper().Map(Furnished()));
        var first = dto.Givens[0];
        dto.Givens[0] = new Dtos.FixedValue
        {
            Id = first.Id, Type = "Variable", Name = first.Name, Dimensionality = first.Dimensionality,
            KmsValue = first.KmsValue, Uncertainty = first.Uncertainty,
        };

        var act = () => new DeserializingMapper(new DeserializationContext()).Map(dto);

        act.Should().Throw<NotImplementedException>();
    }

    /* A system document is refused when it would not build, as S1-9 has it for names: a constant written with
       an uncertainty is not read as a reference value. */
    [Fact]
    public void AConstantWrittenWithUncertaintyIsRefused()
    {
        var dto = ThroughJson(new SerializingMapper().Map(Furnished()));
        var constant = dto.Givens.Single(g => g.Type == "Constant");
        var reference = dto.Givens.Single(g => g.Type == "ReferenceValue");
        dto.Givens[dto.Givens.IndexOf(constant)] = new Dtos.FixedValue
        {
            Id = constant.Id, Type = constant.Type, Name = constant.Name, Dimensionality = constant.Dimensionality,
            KmsValue = constant.KmsValue, Uncertainty = reference.Uncertainty,
        };

        var act = () => new DeserializingMapper(new DeserializationContext()).Map(dto);

        act.Should().Throw<Calcusystem.DimensionedExpression.Exceptions.InvalidFixedValueException>();
    }
}
