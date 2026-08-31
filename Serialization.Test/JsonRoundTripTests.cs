using System.Text.Json;
using Calcusystem.Serialization.Mappers;
using Calcusystem.DimensionedExpression;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using FluentAssertions;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Enums;
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

        return new DeserializingMapper(new DeserializationContext()).Map(revived);
    }

    /// <remarks>
    /// Operator semantics reach the wire as enums the other suites never push through a serializer. The same
    /// trap this class exists for applies: a value that fails to serialize comes back as the enum's zero, and
    /// zero is a legal <c>AgreementRule</c>-shaped value that would silently reinterpret the model. Each of the
    /// three readings is checked, so a default cannot masquerade as the one that was written.
    /// </remarks>
    [Theory]
    [InlineData(AgreementRule.Nominal)]
    [InlineData(AgreementRule.Mutual)]
    [InlineData(AgreementRule.Overlapping)]
    public void AnEqualitysAgreementRuleSurvivesJson(AgreementRule rule)
    {
        var system = ExpressionSystem.Create("json", "equality semantics through a real serializer");
        var lhs = new Variable("a", Mass.Kilogram.Quantity(1).Measurand(SymmetricUncertainty.FromRelErr(0.1)));
        var rhs = new Variable("b", Mass.Kilogram.Quantity(1).Measurand(SymmetricUncertainty.FromRelErr(0.1)));
        system.Add(new EqualityOperator(rule, SolvingRole.Equation) { Id = "eq", Lhs = lhs, Rhs = rhs });

        var restored = RoundTripThroughJson(system);

        restored.Relationships.Single().Should().BeOfType<EqualityOperator>()
            .Which.Agreement.Should().Be(rule);
    }

    /// <remarks>
    /// A simple comparison's whole meaning is its rule, so a rule lost in transit leaves an operator that
    /// compares something other than what was written. Deliberately not the default landmark pair, so a
    /// zeroed-out field cannot pass.
    /// </remarks>
    [Fact]
    public void ASimpleComparisonsRuleSurvivesJson()
    {
        var rule = new ComparisonRule(Landmark.Nominal, ComparisonType.LessThan, Landmark.LowerBound);
        var system = ExpressionSystem.Create("json", "a comparison rule through a real serializer");
        var lhs = new Variable("a", Mass.Kilogram.Quantity(1).Measurand(SymmetricUncertainty.FromRelErr(0.1)));
        var rhs = new Variable("b", Mass.Kilogram.Quantity(1).Measurand(SymmetricUncertainty.FromRelErr(0.1)));
        system.Add(new SimpleComparison(rule) { Id = "c", Lhs = lhs, Rhs = rhs });

        var restored = RoundTripThroughJson(system);

        var comparison = restored.Relationships.Single().Should().BeOfType<SimpleComparison>().Which;
        comparison.Rule.Should().Be(rule);
        comparison.Symbol.Should().Be("·<⌟");
    }

    /// <remarks>
    /// The twelve operators with fixed rules must not acquire either field. A non-null agreement on, say, a
    /// tolerance check would be state describing nothing, and reconstruction would have no reason to reject it.
    /// </remarks>
    [Fact]
    public void AnOperatorWithFixedRulesCarriesNeitherFieldOnTheWire()
    {
        var system = ExpressionSystem.Create("json", "");
        var lhs = new Variable("a", Mass.Kilogram.Quantity(1).Measurand(SymmetricUncertainty.FromRelErr(0.1)));
        var rhs = new Variable("b", Mass.Kilogram.Quantity(1).Measurand(SymmetricUncertainty.FromRelErr(0.1)));
        system.Add(new WhollyWithinToleranceOperator { Id = "w", Lhs = lhs, Rhs = rhs });

        var dto = new SerializingMapper().Map(system).Relationships.Single();

        dto.Agreement.Should().BeNull();
        dto.RuleLhs.Should().BeNull();
        dto.RuleComparison.Should().BeNull();
        dto.RuleRhs.Should().BeNull();
    }

    [Fact]
    public void VariableDimensionalitySurvivesJson()
    {
        var force = Dimensionality.Mass * Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time);

        var system = ExpressionSystem.Create("json", "dimensionality through a real serializer");
        system.Add(new Variable("F", force));

        var restored = RoundTripThroughJson(system);

        var variable = restored.Variables.Single();
        variable.Symbol.Should().Be("F");
        variable.Dimensionality.Should().Be(force);
        variable.Dimensionality.Should().NotBe(Dimensionality.Dimensionless);
    }

    [Fact]
    public void ValueAndUncertaintySurviveJson()
    {
        var system = ExpressionSystem.Create("json", "value and uncertainty through a real serializer");
        system.Add(new Variable(
            "m",
            Mass.Kilogram.Quantity(2).Measurand(AsymmetricUncertainty.FromRelErr(0.05, 0.01))));

        var restored = RoundTripThroughJson(system);

        var value = restored.Variables.Single().Value!;
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
        system.Add(new Variable(
            "x",
            Dimensionality.Length.Quantity(0).Measurand(SymmetricUncertainty.FromAbsErr(
                Length.Meter.Quantity(0.5)))));

        var restored = RoundTripThroughJson(system);

        var value = restored.Variables.Single().Value!;
        value.KmsAbsoluteError.Should().Be(0.5);
        value.RelativeError.Should().Be(double.PositiveInfinity);
    }

    [Fact]
    public void DimensionlessVariableSurvivesJson()
    {
        var system = ExpressionSystem.Create("json", "dimensionless encodes as empty");
        system.Add(new Variable("ratio", Dimensionality.Dimensionless));

        var restored = RoundTripThroughJson(system);

        restored.Variables.Single().Dimensionality.Should().Be(Dimensionality.Dimensionless);
    }
}
