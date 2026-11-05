using Calcusystem.Analysis.Extensions;
using Calcusystem.Examples.Problems;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Examples.Test;

public class ParallelDcLoadsTests
{
    [Fact]
    public void TheBranchAndSupplyCurrentsAreTheHandWorkedOnes()
    {
        var example = ParallelDcLoads.Build();
        var calculation = example.System.Calculate();

        calculation.ValueOf(example.Node("I₁"))!.In(ElectricCurrent.Ampere)
            .Should().BeApproximately(2.000000, 1e-6);

        calculation.ValueOf(example.Node("I₂"))!.In(ElectricCurrent.Ampere)
            .Should().BeApproximately(2.926829, 1e-6);

        calculation.ValueOf(example.Node("I₃"))!.In(ElectricCurrent.Ampere)
            .Should().BeApproximately(0.510638, 1e-6);

        calculation.ValueOf(example.Node(ParallelDcLoads.SupplyCurrentId))!.In(ElectricCurrent.Ampere)
            .Should().BeApproximately(5.437468, 1e-6);

        calculation.ValueOf(example.Node("R_eq"))!.In(ElectricResistance.Ohm)
            .Should().BeApproximately(4.413819, 1e-6);

        calculation.ValueOf(example.Node("P_total"))!.In(Power.Watt)
            .Should().BeApproximately(130.4992, 1e-4);
    }

    /* A value outside a bound it was tested against is a violation, not an inconsistency: the model is
       coherent and the design is not. The two are the same failing verdict and different findings, and which
       one it is turns on whether the relationship names a criterion. */
    [Fact]
    public void ExceedingTheBreakerRatingIsAViolationAndNotAnInconsistency()
    {
        var calculation = ParallelDcLoads.Build().System.Calculate();

        calculation.IsComplete.Should().BeTrue();
        calculation.Inconsistencies.Should().BeEmpty();
        calculation.Violations
            .Should().ContainSingle(outcome => outcome.Relationship.Id == ParallelDcLoads.BreakerCheckId);
    }

    /* The instrument branch passes by about a percent and a half, which is inside the uncertainty of several
       of these quantities but outside its own. "Definitely less than" asks whether the whole interval clears
       the bound, so a pass here is a pass whatever the inputs do within their bars. */
    [Fact]
    public void TheInstrumentFuseHoldsAcrossTheWholeUncertaintyInterval()
    {
        var example = ParallelDcLoads.Build();
        var calculation = example.System.Calculate();

        var current = calculation.ValueOf(example.Node("I₂"))!;
        var rating = example.Leaf("I_fuse2").Value!;

        (current.KmsValue + current.KmsUpperAbsoluteUncertainty)
            .Should().BeLessThan(rating.KmsValue);

        calculation.OutcomeFor(
                example.System.Relationships.Single(r => r.Id == "instrument fuse holds"))!
            .IsSatisfied.Should().BeTrue();
    }

    [Fact]
    public void KirchhoffsCurrentLawHolds()
    {
        var example = ParallelDcLoads.Build();
        var calculation = example.System.Calculate();

        calculation.ValueOf(example.Node("ΣI_branch"))!.In(ElectricCurrent.Ampere)
            .Should().BeApproximately(
                calculation.ValueOf(example.Node(ParallelDcLoads.SupplyCurrentId))!
                    .In(ElectricCurrent.Ampere),
                1e-12);
    }

    /* V·I_total and V²·G are the same number and not the same uncertainty. I_total already holds V_bus, so
       the first form presents one measurement as two independent ones and reports less uncertainty than the
       inputs carry. Gathering the square into its own Correlated node is what states the correlation, and it
       is the only way to: the flag belongs to a node, and one node cannot say "correlated with this factor
       but independent of that one". */
    [Fact]
    public void AFactorRepeatedThroughASubExpressionUnderStatesTheUncertainty()
    {
        var example = ParallelDcLoads.Build();
        var calculation = example.System.Calculate();

        var stated = calculation.ValueOf(example.Node("P_total"))!;
        var naive = ParallelDcLoads.PowerWrittenWithARepeatedFactor(example).ComputeIfFullyDescribed()!;

        naive.In(Power.Watt).Should().BeApproximately(stated.In(Power.Watt), 1e-9);
        naive.RelativeUncertainty.Should().BeLessThan(stated.RelativeUncertainty);

        var bus = example.Leaf("V_bus").Value!.RelativeUncertainty;
        var conductance = calculation.ValueOf(example.Node("G_total"))!.RelativeUncertainty;

        stated.RelativeUncertainty
            .Should().BeApproximately(Math.Sqrt(4 * bus * bus + conductance * conductance), 1e-12);

        naive.RelativeUncertainty
            .Should().BeApproximately(Math.Sqrt(2 * bus * bus + conductance * conductance), 1e-12);
    }
}
