using System;
using DimensionedExpression.Expressions;
using DimensionedExpression.Provenance;
using DimensionedExpression.State;
using FluentAssertions;
using Measurement;
using Xunit;

namespace DimensionedExpression.Test.Provenance;

public class ProvenanceTests
{
    [Fact]
    public void Measured_Summary_IncludesInstrumentAndCalibration()
    {
        var provenance = ProvenanceFactory.Measured("SN-42", new DateOnly(2026, 1, 15));
        provenance.Summary().Should().Be("Measured (instrument SN-42, calibrated 2026-01-15)");
    }

    [Fact]
    public void Measured_Summary_OmitsMissingDetail()
    {
        ProvenanceFactory.Measured().Summary().Should().Be("Measured");
        ProvenanceFactory.Measured("SN-42").Summary().Should().Be("Measured (instrument SN-42)");
    }

    [Fact]
    public void Reference_Summary_IncludesCitationAndYear()
    {
        var provenance = ProvenanceFactory.Reference("NIST SP 811", "https://nist.gov", 2008);
        provenance.Summary().Should().Be("Reference: NIST SP 811 (2008)");
    }

    [Fact]
    public void Design_Summary()
    {
        ProvenanceFactory.Design("DWG-1007").Summary().Should().Be("Design parameter (spec DWG-1007)");
        ProvenanceFactory.Design().Summary().Should().Be("Design parameter");
    }

    [Fact]
    public void Model_Summary()
    {
        ProvenanceFactory.Model("Dittus-Boelter", "fit-2021").Summary()
            .Should().Be("Model parameter: Dittus-Boelter (fit fit-2021)");
    }

    [Fact]
    public void Factory_GeneratesIdByDefault()
    {
        ProvenanceFactory.Design().Id.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FromState_RestoresIdentityAndMetadata()
    {
        // Restoring a persisted identity goes through the state gateway; the creation methods above only ever
        // mint a fresh one, so no caller building a provenance is offered an id parameter.
        var restored = ProvenanceFactory.FromState(
            ProvenanceState.Measured("prov-1", "SN-42", new DateOnly(2026, 1, 15)));

        restored.Id.Should().Be("prov-1");
        restored.Should().BeOfType<MeasuredProvenance>();
        restored.Summary().Should().Be("Measured (instrument SN-42, calibrated 2026-01-15)");
    }

    [Fact]
    public void ProvenanceRoundTripsThroughItsState()
    {
        var original = ProvenanceFactory.Model("Dittus-Boelter", "fit-2021");

        var restored = ProvenanceFactory.FromState(original.GetState());

        restored.Id.Should().Be(original.Id);
        restored.Summary().Should().Be(original.Summary());
        restored.GetState().Should().Be(original.GetState());
    }

    [Fact]
    public void Variable_Provenance_DefaultsToNullAndIsSettable()
    {
        var variable = new Variable("m", Dimensionality.Mass);
        variable.Provenance.Should().BeNull();

        var provenance = ProvenanceFactory.Measured("SN-42");
        variable.Provenance = provenance;
        variable.Provenance.Should().BeSameAs(provenance);
    }

    [Fact]
    public void Variable_Provenance_DoesNotAffectEvaluation()
    {
        var bound = Measurement.Units.Mass.Kilogram.Quantity(2).Measurand(SymmetricUncertainty.FromRelErr(0.01));
        var variable = new Variable("m", bound) { Provenance = ProvenanceFactory.Design() };

        variable.DegreesOfFreedom().Should().Be(0);
        variable.Value!.KmsValue.Should().BeApproximately(2, 1E-9);
    }
}
