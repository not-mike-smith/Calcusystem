using Calcusystem.Analysis.Enums;
using Calcusystem.Analysis.Extensions;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.Examples.Problems;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Examples.Test;

/* The numbers below are worked by hand from the problem statement, not read back off a run. A test that
   recomputes the model's own formula asserts only that arithmetic is deterministic. */
public class OrificeDrainTimeTests
{
    [Fact]
    public void TheDischargeAndDrainTimeAreTheHandWorkedOnes()
    {
        var example = OrificeDrainTime.Build();
        var calculation = example.System.Calculate();

        calculation.ValueOf(example.Node("A₀"))!.In(Area.SquareMeter)
            .Should().BeApproximately(1.963495e-3, 1e-9);

        calculation.ValueOf(example.Node("v_jet"))!.In(Speed.MeterPerSecond)
            .Should().BeApproximately(7.922282, 1e-6);

        calculation.ValueOf(example.Node(OrificeDrainTime.InitialDischargeId))!
            .In(VolumetricFlow.GallonPerMinute)
            .Should().BeApproximately(150.4001, 1e-4);

        calculation.ValueOf(example.Node(OrificeDrainTime.DrainTimeId))!.In(Time.Minute)
            .Should().BeApproximately(50.85469, 1e-5);
    }

    /* The two routes to the mean discharge reduce to the same expression, so a disagreement means the tree was
       built wrong rather than that the data conflicts. Uncertainty is the part that does not survive the
       rearrangement - see TheTwoRoutesCarryDifferentUncertainty below. */
    [Fact]
    public void BothRoutesToTheMeanDischargeAgree()
    {
        var example = OrificeDrainTime.Build();
        var calculation = example.System.Calculate();

        var fromVolume = calculation.ValueOf(example.Node("V_tank/t_drain"))!;
        var halfInitial = calculation.ValueOf(example.Node("Q₀/2"))!;

        fromVolume.In(VolumetricFlow.GallonPerMinute)
            .Should().BeApproximately(halfInitial.In(VolumetricFlow.GallonPerMinute), 1e-9);

        calculation.Inconsistencies.Should().BeEmpty();
    }

    /* Uncertainty is not invariant under algebraic rearrangement. Both routes reach C_d·A₀·√(gh₀/2), but the
       longer one passes the orifice area and the discharge coefficient through twice, and the propagator has
       no way to know the two appearances are one measurement. The nominal values still agree exactly, which
       is why a coherence check reads them nominally. */
    [Fact]
    public void TheTwoRoutesCarryDifferentUncertainty()
    {
        var example = OrificeDrainTime.Build();
        var calculation = example.System.Calculate();

        var fromVolume = calculation.ValueOf(example.Node("V_tank/t_drain"))!;
        var halfInitial = calculation.ValueOf(example.Node("Q₀/2"))!;

        fromVolume.RelativeUncertainty.Should().BeGreaterThan(halfInitial.RelativeUncertainty);
    }

    /* The correlation flag belongs to a node, so a repeated input has to be gathered into one of its own.
       Flat, with the diameter listed twice beside an exact factor, neither setting is right: Uncorrelated
       treats one measurement as two independent ones, and Correlated would be right only because the third
       factor contributes zero. */
    [Fact]
    public void SquaringAMeasurementDoublesItsRelativeUncertainty()
    {
        var example = OrificeDrainTime.Build();
        var diameter = example.Leaf("D");
        var once = diameter.Value!.RelativeUncertainty;

        var calculation = example.System.Calculate();

        calculation.ValueOf(example.Node("D²"))!.RelativeUncertainty
            .Should().BeApproximately(2 * once, 1e-12);

        new ProductExpression([diameter, diameter]) { Id = "D² as independent" }
            .ComputeIfFullyDescribed()!
            .RelativeUncertainty
            .Should().BeApproximately(Math.Sqrt(2) * once, 1e-12);
    }

    [Fact]
    public void EveryCheckHoldsAndNothingIsLeftOutstanding()
    {
        var calculation = OrificeDrainTime.Build().System.Calculate();

        calculation.IsComplete.Should().BeTrue();
        calculation.AllRelationshipsHold.Should().BeTrue();
        calculation.Undetermined.Should().BeEmpty();
        calculation.Outcomes.Should().HaveCount(4);
    }

    /* Every value is supplied, so the equalities in the model compare quantities that are already known. They
       determine nothing and are reported as redundancy rather than counted against the unknowns. */
    [Fact]
    public void AFullySuppliedModelIsExactlyDeterminedWithItsEqualitiesRedundant()
    {
        var flat = OrificeDrainTime.Build().System.Flatten();

        flat.Unknowns.Should().BeEmpty();
        flat.DegreesOfFreedom.Should().Be(0);
        flat.Determination.Should().Be(Determination.ExactlyDetermined);
        flat.RedundantEquations.Should().HaveCount(2);
    }

    [Fact]
    public void WithholdingAValueLeavesTheChecksOutstandingRatherThanFailed()
    {
        var example = OrificeDrainTime.Build();
        example.Leaf(OrificeDrainTime.DischargeCoefficientId).Value = null;

        var calculation = example.System.Calculate();

        calculation.IsComplete.Should().BeFalse();
        calculation.MissingValues
            .Should().ContainSingle(variable => variable.Id == OrificeDrainTime.DischargeCoefficientId);

        calculation.Undetermined.Should().HaveCount(4);
        calculation.Violations.Should().BeEmpty();
        calculation.Inconsistencies.Should().BeEmpty();
    }

    /* Nodes above the gap stop resolving; nodes beside it do not. The areas depend on the geometry alone, so
       they still compute - which is what makes a half-built model worth calculating at all. */
    [Fact]
    public void WithholdingAValueStopsOnlyWhatDependsOnIt()
    {
        var example = OrificeDrainTime.Build();
        example.Leaf(OrificeDrainTime.DischargeCoefficientId).Value = null;

        var calculation = example.System.Calculate();

        calculation.ValueOf(example.Node("A_tank")).Should().NotBeNull();
        calculation.ValueOf(example.Node("v_jet")).Should().NotBeNull();
        calculation.ValueOf(example.Node(OrificeDrainTime.InitialDischargeId)).Should().BeNull();
        calculation.ValueOf(example.Node(OrificeDrainTime.DrainTimeId)).Should().BeNull();
    }

    /* Both coherence checks reach the discharge coefficient once it is unknown, so the model holds more
       information about it than determining it needs. That surplus is what would let it be backed out of the
       metered flow, and a count alone would only report the system as over-determined. */
    [Fact]
    public void WithholdingTheDischargeCoefficientLeavesItOverDetermined()
    {
        var example = OrificeDrainTime.Build();
        example.Leaf(OrificeDrainTime.DischargeCoefficientId).Value = null;

        var flat = example.System.Flatten();

        flat.DegreesOfFreedom.Should().Be(-1);
        flat.Determination.Should().Be(Determination.Overdetermined);

        flat.Decompose().Overdetermined.Unknowns
            .Should().ContainSingle(variable => variable.Id == OrificeDrainTime.DischargeCoefficientId);
    }
}
