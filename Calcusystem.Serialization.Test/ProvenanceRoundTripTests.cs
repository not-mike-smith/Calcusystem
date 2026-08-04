using System;
using System.Linq;
using Calcusystem.Serialization.Mappers;
using DimensionedExpression.BinaryOperators;
using DimensionedExpression.Expressions;
using DimensionedExpression.Interfaces;
using DimensionedExpression.Provenance;
using DimensionedExpression.Systems;
using FluentAssertions;
using Measurement;
using Measurement.Models;
using Measurement.Uncertainty;

namespace Calcusystem.Serialization.Test;

public class ProvenanceRoundTripTests
{
    private static ExpressionSystem RoundTrip(ExpressionSystem system)
    {
        var dto = new SerializingMapper().Map(system);
        var mapper = new DeserializingMapper(new DeserializationContext(), new AlwaysEqual());
        return mapper.Map(dto);
    }

    [Fact]
    public void Variable_Provenance_RoundTrips()
    {
        var system = ExpressionSystem.Create("provenance", "variable provenance");
        var measured = new Variable(
            "m",
            new Quantity(2, Dimensionality.Mass).Measurand(GaussianUncertainty.FromRelErr(0.01)),
            "m")
        {
            Provenance = ProvenanceFactory.Measured("SN-42", new DateOnly(2026, 1, 15), "prov-m")
        };
        system.DirectExpressions.Add(measured);

        var restored = (Variable)RoundTrip(system).GetAllExpressions().Single(e => e.Id == "m");

        restored.Provenance.Should().BeOfType<MeasuredProvenance>();
        var provenance = (MeasuredProvenance)restored.Provenance!;
        provenance.Id.Should().Be("prov-m");
        provenance.InstrumentId.Should().Be("SN-42");
        provenance.CalibrationDate.Should().Be(new DateOnly(2026, 1, 15));
        provenance.Summary().Should().Be(measured.Provenance!.Summary());
    }

    [Fact]
    public void Variable_WithoutProvenance_RoundTripsAsNull()
    {
        var system = ExpressionSystem.Create("no-prov", "");
        system.DirectExpressions.Add(new Variable("x", Dimensionality.Length, "x"));

        var restored = (Variable)RoundTrip(system).GetAllExpressions().Single(e => e.Id == "x");
        restored.Provenance.Should().BeNull();
    }

    [Fact]
    public void Operator_Provenance_RoundTrips()
    {
        var system = ExpressionSystem.Create("operator-provenance", "");
        var lhs = new Variable("x", Dimensionality.Length, "x");
        var rhs = new Variable("y", Dimensionality.Length, "y");
        system.DirectExpressions.Add(lhs);
        system.DirectExpressions.Add(rhs);

        system.Definitions.Add(new WithinBindingToleranceOperator
        {
            Id = "op",
            Lhs = lhs,
            Rhs = rhs,
            Provenance = ProvenanceFactory.Reference("NIST SP 811", "https://nist.gov", 2008, "prov-op")
        });

        var restored = RoundTrip(system).Definitions.Single();

        restored.Provenance.Should().BeOfType<ReferenceProvenance>();
        var provenance = (ReferenceProvenance)restored.Provenance!;
        provenance.Id.Should().Be("prov-op");
        provenance.Citation.Should().Be("NIST SP 811");
        provenance.Url.Should().Be("https://nist.gov");
        provenance.Year.Should().Be(2008);
    }

    private sealed class AlwaysEqual : IEqualityEstimating
    {
        public bool AreEqual(Measurand lhs, Measurand rhs) => true;
    }
}
