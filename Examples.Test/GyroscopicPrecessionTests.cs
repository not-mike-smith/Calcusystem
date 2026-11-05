using Calcusystem.Analysis.Extensions;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.Examples.Problems;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Examples.Test;

public class GyroscopicPrecessionTests
{
    [Fact]
    public void ThePrecessionRateAndPeriodAreTheHandWorkedOnes()
    {
        var example = GyroscopicPrecession.Build();
        var calculation = example.System.Calculate();

        calculation.ValueOf(example.Node("I"))!.In(MomentOfInertia.KilogramMeterSquared)
            .Should().BeApproximately(0.01728, 1e-8);

        calculation.ValueOf(example.Node("L_s"))!.In(AngularMomentum.KilogramMeterSquaredPerSecond)
            .Should().BeApproximately(6.514407, 1e-6);

        calculation.ValueOf(example.Node(GyroscopicPrecession.PrecessionRateId))!.In(AngularVelocity.Rpm)
            .Should().BeApproximately(1.078150, 1e-6);

        calculation.ValueOf(example.Node(GyroscopicPrecession.PrecessionRateId))!
            .In(AngularVelocity.DegreePerSecond)
            .Should().BeApproximately(6.468900, 1e-6);

        calculation.ValueOf(example.Node("T_p"))!.In(Time.Second)
            .Should().BeApproximately(55.65088, 1e-5);
    }

    /* Torque carries one angle here and angular momentum carries one, so tau over I-omega cancels both and
       lands on T⁻¹ - a frequency, which cannot be read in rpm or degrees per second. The radian the cross
       product consumes is what makes the quotient an angular velocity again. It changes no number, because
       Angle.Radian converts to KMS by one; it changes which units the answer can be expressed in. */
    [Fact]
    public void TheExplicitRadianChangesTheDimensionAndNotTheNumber()
    {
        var example = GyroscopicPrecession.Build();
        var calculation = example.System.Calculate();

        var withRadian = example.Node(GyroscopicPrecession.PrecessionRateId);
        var withoutRadian = new QuotientExpression
        {
            Id = "τ/L_s",
            Numerator = example.Leaf("τ"),
            Denominator = example.Node("L_s"),
        };

        withRadian.Dimensionality.Should().Be(AngularVelocity.RadPerSecond.Dimensionality);
        withoutRadian.Dimensionality.Should().Be(Frequency.Hertz.Dimensionality);

        calculation.ValueOf(withRadian)!.KmsValue
            .Should().BeApproximately(withoutRadian.ComputeIfFullyDescribed()!.KmsValue, 1e-15);

        example.Leaf(GyroscopicPrecession.RadianConsumedByTheCrossProductId).Value!.KmsValue
            .Should().Be(1);
    }

    /* The radius is one measurement used twice and the mass is a separate one, so the two combine by
       different rules. Written as one flat product neither rule can apply to both: in quadrature throughout
       gives 0.77 %, summed throughout gives 1.33 %, and the right answer is neither. */
    [Fact]
    public void TheInertiaCombinesASquaredRadiusWithAnIndependentMass()
    {
        var example = GyroscopicPrecession.Build();
        var calculation = example.System.Calculate();

        var mass = example.Leaf("m").Value!.RelativeUncertainty;
        var radius = example.Leaf("r").Value!.RelativeUncertainty;

        calculation.ValueOf(example.Node("I"))!.RelativeUncertainty
            .Should().BeApproximately(Math.Sqrt(mass * mass + 4 * radius * radius), 1e-12);
    }

    /* A requirement on the model rather than on the design: the steady-precession formula describes the
       motion only while the precession is slow against the spin, so the answer is checked against the
       assumption that produced it. */
    [Fact]
    public void TheSlowPrecessionAssumptionHolds()
    {
        var example = GyroscopicPrecession.Build();
        var calculation = example.System.Calculate();

        calculation.AllRelationshipsHold.Should().BeTrue();
        calculation.Violations.Should().BeEmpty();

        calculation.ValueOf(example.Node("Ω_max"))!.In(AngularVelocity.Rpm)
            .Should().BeApproximately(36.0, 1e-9);
    }
}
