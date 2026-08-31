using System;
using System.Linq;
using Calcusystem.Serialization.Mappers;
using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.Systems;
using FluentAssertions;
using Calcusystem.Measurement;

namespace Calcusystem.Serialization.Test;

public class ProvenanceRoundTripTests
{
    private static ExpressionSystem RoundTrip(ExpressionSystem system)
    {
        var dto = new SerializingMapper().Map(system);
        var mapper = new DeserializingMapper(new DeserializationContext());
        return mapper.Map(dto);
    }

    [Fact]
    public void Variable_Provenance_RoundTrips()
    {
        var system = ExpressionSystem.Create("provenance", "variable provenance");
        var measured = new Variable(
            "m",
            new Quantity(2, Dimensionality.Mass).Measurand(SymmetricUncertainty.FromRelErr(0.01)),
            "m")
        {
            Provenance = ProvenanceFactory.FromState(
                ProvenanceState.Measured("prov-m", "SN-42", new DateOnly(2026, 1, 15)))
        };
        system.Add(measured);

        var restored = (Variable)RoundTrip(system).GetAllExpressions().Single(e => e.Id == "m");

        restored.Provenance.Should().BeOfType<MeasuredProvenance>();
        var state = restored.Provenance!.GetState();
        state.Id.Should().Be("prov-m");
        state.Kind.Should().Be(ProvenanceKind.Measured);
        state.InstrumentId.Should().Be("SN-42");
        state.CalibrationDate.Should().Be(new DateOnly(2026, 1, 15));
        restored.Provenance.Summary().Should().Be(measured.Provenance!.Summary());
    }

    [Fact]
    public void Variable_WithoutProvenance_RoundTripsAsNull()
    {
        var system = ExpressionSystem.Create("no-prov", "");
        system.Add(new Variable("x", Dimensionality.Length, "x"));

        var restored = (Variable)RoundTrip(system).GetAllExpressions().Single(e => e.Id == "x");
        restored.Provenance.Should().BeNull();
    }

    [Fact]
    public void Operator_Provenance_RoundTrips()
    {
        var system = ExpressionSystem.Create("operator-provenance", "");
        var lhs = new Variable("x", Dimensionality.Length, "x");
        var rhs = new Variable("y", Dimensionality.Length, "y");
        system.Add(lhs);
        system.Add(rhs);

        system.Add(new WithinBindingToleranceOperator
        {
            Id = "op",
            Lhs = lhs,
            Rhs = rhs,
            Provenance = ProvenanceFactory.FromState(
                ProvenanceState.Reference("prov-op", "NIST SP 811", "https://nist.gov", 2008))
        });

        var restored = RoundTrip(system).Relationships.Single();

        restored.Provenance.Should().BeOfType<ReferenceProvenance>();
        var state = restored.Provenance!.GetState();
        state.Id.Should().Be("prov-op");
        state.Kind.Should().Be(ProvenanceKind.Reference);
        state.Citation.Should().Be("NIST SP 811");
        state.Url.Should().Be("https://nist.gov");
        state.Year.Should().Be(2008);
    }
}
