using System.Text.Json;
using Calcusystem.Serialization.Mappers;
using Calcusystem.DimensionedExpression;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using FluentAssertions;
using Calcusystem.Measurement;

namespace Calcusystem.Serialization.Test;

/// <summary>
/// What a relationship does to the problem is carried by the operator's <c>SolvingRole</c>, not by which list it
/// was filed under. These tests pin that the role survives a round trip <i>without collapsing</i>, that the
/// system's views partition on it, and that an operator which cannot determine never comes back claiming it does.
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
        return new DeserializingMapper(new DeserializationContext()).Map(dto);
    }

    [Theory]
    [InlineData(SolvingRole.Equation)]
    [InlineData(SolvingRole.Coherence)]
    [InlineData(SolvingRole.Requirement)]
    public void EqualityKeepsItsSolvingRoleThroughJson(SolvingRole role)
    {
        var system = TwoLeafSystem(out var lhs, out var rhs);
        system.Add(new EqualityOperator(AgreementRule.Nominal, role) { Id = "eq", Lhs = lhs, Rhs = rhs });

        var restored = FromJson(ToJson(system));

        restored.Relationships.Single().SolvingRole.Should().Be(role);
    }

    /// <remarks>
    /// The reason the wire carries the role rather than the derived <c>IsDetermining</c> boolean. Both of these
    /// are determining, so a boolean would write <c>true</c> for each and there would be no way to tell an
    /// equation from a coherence assertion on the way back in.
    /// </remarks>
    [Fact]
    public void EquationAndCoherenceStayDistinctThroughJson()
    {
        var system = TwoLeafSystem(out var lhs, out var rhs);
        system.Add(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Equation)
        {
            Id = "defines", Lhs = lhs, Rhs = rhs
        });
        system.Add(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Coherence)
        {
            Id = "agrees", Lhs = lhs, Rhs = rhs
        });

        var restored = FromJson(ToJson(system));

        restored.Relationships.Should().OnlyContain(r => r.IsDetermining);
        restored.Equations.Select(e => e.Id).Should().Equal("defines");
        restored.CoherenceChecks.Select(c => c.Id).Should().Equal("agrees");
    }

    [Fact]
    public void ViewsPartitionRelationshipsByRole()
    {
        var system = TwoLeafSystem(out var lhs, out var rhs);
        system.Add(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Equation)
        {
            Id = "defn", Lhs = lhs, Rhs = rhs
        });
        system.Add(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Coherence)
        {
            Id = "cohere", Lhs = lhs, Rhs = rhs
        });
        system.Add(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Requirement)
        {
            Id = "check", Lhs = lhs, Rhs = rhs
        });
        system.Add(new WithinBindingToleranceOperator { Id = "tol", Lhs = lhs, Rhs = rhs });

        // The same operator type appears in all three views — the role does the partitioning, not the type.
        system.Equations.Select(x => x.Id).Should().Equal("defn");
        system.CoherenceChecks.Select(x => x.Id).Should().Equal("cohere");
        system.Requirements.Select(x => x.Id).Should().Equal("check", "tol");
    }

    [Fact]
    public void NonEqualityOperatorsAreAlwaysRequirements()
    {
        var system = TwoLeafSystem(out var lhs, out var rhs);
        system.Add(new WithinBindingToleranceOperator { Id = "tol", Lhs = lhs, Rhs = rhs });
        system.Add(new DefinitelyLessThanOperator { Id = "lt", Lhs = lhs, Rhs = rhs });

        system.Relationships.Should().OnlyContain(r => r.SolvingRole == SolvingRole.Requirement);
        system.Relationships.Should().OnlyContain(r => ! r.IsDetermining);
        system.Equations.Should().BeEmpty();
        system.CoherenceChecks.Should().BeEmpty();
    }

    /// <remarks>
    /// A role can only survive if the operator on the other end can represent it. Reconstruction dispatches on
    /// the state's kind, so this pins that a non-equality operator comes back a requirement rather than picking
    /// the role up from a payload claiming otherwise — the wire cannot promote a check into an equation.
    /// </remarks>
    [Fact]
    public void NonEqualityOperatorsStayRequirementsThroughATamperedPayload()
    {
        var system = TwoLeafSystem(out var lhs, out var rhs);
        system.Add(new WithinBindingToleranceOperator { Id = "tol", Lhs = lhs, Rhs = rhs });

        var json = ToJson(system)
            .Replace($"\"SolvingRole\":{(byte)SolvingRole.Requirement}",
                     $"\"SolvingRole\":{(byte)SolvingRole.Equation}");
        json.Should().Contain($"\"SolvingRole\":{(byte)SolvingRole.Equation}", "the tamper must actually apply");

        var restored = FromJson(json);

        restored.Relationships.Single().SolvingRole.Should().Be(SolvingRole.Requirement);
        restored.Relationships.Single().IsDetermining.Should().BeFalse();
        restored.Equations.Should().BeEmpty();
        restored.Requirements.Should().HaveCount(1);
    }
}
