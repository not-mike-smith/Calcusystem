using Calcusystem.Analysis.Extensions;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Examples.Presentation;
using Calcusystem.Examples.Problems;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Examples.Test;

/* The snippets printed in this assembly's READMEs, compiled. Documentation that no longer builds is worse
   than none, and prose alone cannot show that a shown `using` list is sufficient. */
public class ReadmeSnippets
{
    [Fact]
    public void ProblemsReadmeStartHere()
    {
        var example = OrificeDrainTime.Build();
        var calculation = example.System.Calculate();

        // The README shows these as bare expressions, the way the root README does. Asserting them is how
        // this file holds the quoted values to the code.
        calculation.ValueOf(example.Node(OrificeDrainTime.DrainTimeId))!.In(Time.Minute)
            .Should().BeApproximately(50.85, 0.01);

        calculation.Violations.Should().BeEmpty();
    }

    [Fact]
    public void ProblemsReadmeWithholdingAValue()
    {
        var example = OrificeDrainTime.Build();

        example.Leaf(OrificeDrainTime.DischargeCoefficientId).Value = null;

        example.System.Calculate().IsComplete.Should().BeFalse();
    }

    /* The nested-correlation snippet from the assembly README, with the leaves it refers to supplied. The
       three percentages quoted beside it are asserted here so the prose cannot drift from the arithmetic. */
    [Fact]
    public void AssemblyReadmeGatheringARepeatedInput()
    {
        var rotorRadius = new Variable("r", Length.Meter.Quantity(0.1200).WithUncertainty(0.0005.Units(Length.Meter)));
        var rotorMass = new Variable("m", Mass.Kilogram.Quantity(2.400).WithUncertainty(0.5.Percent()));
        var half = new Variable("½", Dimensionless.One.Quantity(0.5).WithoutUncertainty());

        var radiusSquared = new ProductExpression([rotorRadius, rotorRadius])
        {
            Id = "r²",
            UncertaintyCorrelation = UncertaintyCorrelation.Correlated,   // one measurement, used twice
        };

        var rotorInertia = new ProductExpression([half, rotorMass, radiusSquared]);   // independent, so quadrature

        var flat = new ProductExpression([half, rotorMass, rotorRadius, rotorRadius]);

        Percent(rotorInertia).Should().BeApproximately(0.97, 0.005);

        flat.UncertaintyCorrelation = UncertaintyCorrelation.Uncorrelated;
        Percent(flat).Should().BeApproximately(0.77, 0.005);

        flat.UncertaintyCorrelation = UncertaintyCorrelation.Correlated;
        Percent(flat).Should().BeApproximately(1.33, 0.005);
    }

    /* The two routes to the mean discharge: the same number, and not the same uncertainty. */
    [Fact]
    public void AssemblyReadmeTheTwoRoutesAgreeOnTheValueAndNotTheBar()
    {
        var example = OrificeDrainTime.Build();
        var calculation = example.System.Calculate();

        var fromVolume = calculation.ValueOf(example.Node("V_tank/t_drain"))!;
        var halfInitial = calculation.ValueOf(example.Node("Q₀/2"))!;

        var apart = Math.Abs(fromVolume.KmsValue - halfInitial.KmsValue) / halfInitial.KmsValue;
        apart.Should().BeLessThan(1e-12, "nominal agreement is what a coherence check reads");

        fromVolume.RelativeUncertainty.Should().BeGreaterThan(halfInitial.RelativeUncertainty);
    }

    [Fact]
    public void PresentationReadmeStartHere()
    {
        var example = OrificeDrainTime.Build();

        ExampleReport.Render(example).Should().NotBeEmpty();
        ExampleReport.Render(example, example.System.Calculate()).Should().NotBeEmpty();
    }

    private static double Percent(IExpression expression) =>
        expression.ComputeIfFullyDescribed()!.RelativeUncertainty * 100;
}
