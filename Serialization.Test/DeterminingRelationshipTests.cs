using System.Text.Json;
using Calcusystem.Serialization.Mappers;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using FluentAssertions;
using Calcusystem.Measurement;

namespace Calcusystem.Serialization.Test;

/// <summary>
/// Whether a relationship defines a value or merely checks one is carried by the operator's
/// <c>IsDetermining</c>, not by which list it was filed under. These tests pin that the flag survives a round
/// trip, that the <c>Definitions</c>/<c>Constraints</c> views partition on it, and that an operator which
/// cannot determine never comes back claiming it does.
/// </summary>
public class DeterminingRelationshipTests
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    private static Variable Bound(string symbol, double kmsValue) =>
        new(symbol,
            new Quantity(kmsValue, Dimensionality.Length).Measurand(SymmetricUncertainty.FromRelErr(0.01)),
            symbol);

    private static ExpressionSystem TwoLeafSystem(out Variable lhs, out Variable rhs)
    {
        var system = ExpressionSystem.Create("determining", "");
        lhs = Bound("x", 10);
        rhs = Bound("y", 10);
        system.Add(lhs);
        system.Add(rhs);
        return system;
    }

    private static string ToJson(ExpressionSystem system) =>
        JsonSerializer.Serialize(new SerializingMapper().Map(system), Options);

    private static ExpressionSystem FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<Dtos.ExpressionSystem>(json, Options)!;
        return new DeserializingMapper(new DeserializationContext(), new AlwaysEqual()).Map(dto);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EqualityKeepsItsDeterminingFlagThroughJson(bool isDetermining)
    {
        var system = TwoLeafSystem(out var lhs, out var rhs);
        system.Add(new EqualityOperator(new AlwaysEqual(), isDetermining)
        {
            Id = "eq", Lhs = lhs, Rhs = rhs
        });

        var restored = FromJson(ToJson(system));

        restored.Relationships.Single().IsDetermining.Should().Be(isDetermining);
    }

    [Fact]
    public void ViewsPartitionRelationshipsByTheFlag()
    {
        var system = TwoLeafSystem(out var lhs, out var rhs);
        system.Add(new EqualityOperator(new AlwaysEqual(), isDetermining: true)
        {
            Id = "defn", Lhs = lhs, Rhs = rhs
        });
        system.Add(new EqualityOperator(new AlwaysEqual(), isDetermining: false)
        {
            Id = "check", Lhs = lhs, Rhs = rhs
        });
        system.Add(new WithinBindingToleranceOperator { Id = "tol", Lhs = lhs, Rhs = rhs });

        // Same operator type on both sides of the split — the flag is doing the partitioning, not the type.
        system.Definitions.Select(d => d.Id).Should().Equal("defn");
        system.Constraints.Select(c => c.Id).Should().Equal("check", "tol");
    }

    [Fact]
    public void NonEqualityOperatorsAreNeverDetermining()
    {
        var system = TwoLeafSystem(out var lhs, out var rhs);
        system.Add(new WithinBindingToleranceOperator { Id = "tol", Lhs = lhs, Rhs = rhs });
        system.Add(new DefinitelyLessThanOperator { Id = "lt", Lhs = lhs, Rhs = rhs });

        system.Relationships.Should().OnlyContain(r => r.IsDetermining == false);
        system.Definitions.Should().BeEmpty();
    }

    /// <remarks>
    /// A determining flag can only survive if the operator on the other end can represent it. Reconstruction
    /// dispatches on the state's kind, so this pins that a non-equality operator comes back non-determining
    /// rather than picking the flag up from a payload that claims otherwise.
    /// </remarks>
    [Fact]
    public void NonEqualityOperatorsStayNonDeterminingThroughJson()
    {
        var system = TwoLeafSystem(out var lhs, out var rhs);
        system.Add(new WithinBindingToleranceOperator { Id = "tol", Lhs = lhs, Rhs = rhs });

        var json = ToJson(system).Replace("\"IsDetermining\":false", "\"IsDetermining\":true");
        var restored = FromJson(json);

        restored.Relationships.Single().IsDetermining.Should().BeFalse();
        restored.Definitions.Should().BeEmpty();
        restored.Constraints.Should().HaveCount(1);
    }

    private sealed class AlwaysEqual : IEqualityEstimating
    {
        public bool AreEqual(Measurand lhs, Measurand rhs) => true;
    }
}
