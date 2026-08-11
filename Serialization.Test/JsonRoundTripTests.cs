using System.Text.Json;
using Calcusystem.Serialization.Mappers;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using FluentAssertions;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Units;

namespace Calcusystem.Serialization.Test;

/// <summary>
/// The other round-trip suites map object-to-object and stop there, which is what let a DTO carrying an
/// unserializable type go unnoticed: <c>SingleVariable.Dimensionality</c> was the <c>Dimensionality</c> struct,
/// whose exponent map is private, so <c>System.Text.Json</c> wrote <c>{}</c> and read back a dimensionless value
/// with no error at all. These tests push the DTOs through an actual serializer so that trap cannot reappear.
/// </summary>
public class JsonRoundTripTests
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    private static ExpressionSystem RoundTripThroughJson(ExpressionSystem system)
    {
        var dto = new SerializingMapper().Map(system);

        var json = JsonSerializer.Serialize(dto, Options);
        var revived = JsonSerializer.Deserialize<Dtos.ExpressionSystem>(json, Options)!;

        return new DeserializingMapper(new DeserializationContext(), new AlwaysEqual()).Map(revived);
    }

    [Fact]
    public void VariableDimensionalitySurvivesJson()
    {
        var force = Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

        var system = ExpressionSystem.Create("json", "dimensionality through a real serializer");
        system.DirectExpressions.Add(new Variable("F", force));

        var restored = RoundTripThroughJson(system);

        var variable = restored.DirectExpressions.Single();
        variable.Symbol.Should().Be("F");
        variable.Dimensionality.Should().Be(force);
        variable.Dimensionality.Should().NotBe(Dimensionality.Dimensionless);
    }

    [Fact]
    public void ValueAndUncertaintySurviveJson()
    {
        var system = ExpressionSystem.Create("json", "value and uncertainty through a real serializer");
        system.DirectExpressions.Add(new Variable(
            "m",
            Mass.Kilogram.Quantity(2).Measurand(AsymmetricUncertainty.FromRelErr(0.05, 0.01))));

        var restored = RoundTripThroughJson(system);

        var value = restored.DirectExpressions.Single().Value!;
        value.In(Mass.Kilogram).Should().Be(2);
        value.UpperRelativeError.Should().Be(0.05);
        value.LowerRelativeError.Should().Be(0.01);
    }

    [Fact]
    public void AbsoluteUncertaintyKeepsItsStorageFormThroughJson()
    {
        // An absolute error is the only form that stays meaningful at zero; if JSON silently converted it to a
        // relative one, RelativeError(0) would be the only symptom.
        var system = ExpressionSystem.Create("json", "absolute error at zero");
        system.DirectExpressions.Add(new Variable(
            "x",
            Dimensionality.Length.Quantity(0).Measurand(SymmetricUncertainty.FromAbsErr(
                Length.Meter.Quantity(0.5)))));

        var restored = RoundTripThroughJson(system);

        var value = restored.DirectExpressions.Single().Value!;
        value.KmsAbsoluteError.Should().Be(0.5);
        value.RelativeError.Should().Be(double.PositiveInfinity);
    }

    [Fact]
    public void DimensionlessVariableSurvivesJson()
    {
        var system = ExpressionSystem.Create("json", "dimensionless encodes as empty");
        system.DirectExpressions.Add(new Variable("ratio", Dimensionality.Dimensionless));

        var restored = RoundTripThroughJson(system);

        restored.DirectExpressions.Single().Dimensionality.Should().Be(Dimensionality.Dimensionless);
    }

    private sealed class AlwaysEqual : IEqualityEstimating
    {
        public bool AreEqual(Measurand lhs, Measurand rhs) => true;
    }
}
