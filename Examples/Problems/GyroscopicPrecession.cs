using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Examples.Presentation;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Units;

namespace Calcusystem.Examples.Problems;

/// <summary>
/// Mechanical engineering: a gimbal-mounted flywheel under a steady torque, and the rate it precesses at.
/// </summary>
/// <remarks>
/// This is the example that pays for angle being a dimension here. <c>Ω = τ/(I·ω)</c> comes out in <c>T⁻¹</c>,
/// not in <c>rad·s⁻¹</c>, because torque carries one angle and angular momentum carries one, and the quotient
/// cancels both. A precession rate is an angular velocity, so the model states the radian the relation
/// consumes — see <c>RadianConsumedByTheCrossProductId</c>.
/// </remarks>
public static class GyroscopicPrecession
{
    /// <summary>The id of the computed precession rate.</summary>
    public const string PrecessionRateId = "Ω_p";

    /// <summary>
    /// The id of the explicit radian that makes <c>τ/(I·ω)</c> an angular velocity rather than a frequency.
    /// </summary>
    /// <remarks>
    /// Exactly 1 rad, so it changes no number — <c>Angle.Radian</c> has a KMS conversion factor of 1. What it
    /// changes is the dimension, and therefore which units the answer can be read in.
    /// </remarks>
    public const string RadianConsumedByTheCrossProductId = "rad";

    /// <summary>Builds the model, the check against the timed precession, and the validity requirement.</summary>
    public static WorkedExample Build()
    {
        // ─── given ────────────────────────────────────────────────────────────────────────────────────────
        var rotorMass = Inputs.Known(
            "m", 2.400, Mass.Kilogram, 0.5.Percent(),
            ProvenanceFactory.Measured("bench scale BS-2", new DateOnly(2026, 5, 20)));

        var rotorRadius = Inputs.Known(
            "r", 0.1200, Length.Meter, 0.0005.Units(Length.Meter),
            ProvenanceFactory.Measured("micrometer M-7", new DateOnly(2026, 4, 1)));

        var spinRate = Inputs.Known(
            "ω_s", 3600, AngularVelocity.Rpm, 1.0.Percent(),
            ProvenanceFactory.Measured("optical tachometer T-3", new DateOnly(2026, 5, 20)));

        var appliedTorque = Inputs.Known(
            "τ", 0.7355, Torque.NewtonMeter, 1.5.Percent(),
            ProvenanceFactory.Measured("gimbal load cell LC-4", new DateOnly(2026, 5, 18)));

        var timedPrecession = Inputs.Known(
            "Ω_timed", 6.40, AngularVelocity.DegreePerSecond, 4.0.Percent(),
            ProvenanceFactory.Measured("stopwatch over 10 revolutions"));

        // The one radian the cross product consumes. Exact, and numerically 1.
        var radian = Inputs.Exact(
            RadianConsumedByTheCrossProductId, 1, Angle.Radian,
            ProvenanceFactory.Reference("SI Brochure 9th ed., §2.3.3 — the radian is a derived unit of one"));

        var revolution = Inputs.Exact("1 rev", 1, Angle.Revolution);
        var half = Inputs.Exact("½", 0.5, Dimensionless.One);

        // Above a few percent of the spin rate the steady-precession formula stops describing the motion:
        // nutation appears and the rotor no longer behaves as a rigid gyroscope under slow torque.
        var validityFraction = Inputs.Exact(
            "1 %", 0.0100, Dimensionless.One,
            ProvenanceFactory.Design("slow-precession validity limit for this rig"));

        // ─── rotor inertia: I = ½·m·r² ────────────────────────────────────────────────────────────────────
        // The radius squared gets its own Correlated node: it is one measurement used twice, so its relative
        // uncertainty doubles. The mass is a separate measurement, so the node above combines in quadrature.
        // Written as a single flat product the two rules would collide and neither answer would be right.
        var radiusSquared = new ProductExpression([rotorRadius, rotorRadius])
        {
            Id = "r²",
            UncertaintyCorrelation = UncertaintyCorrelation.Correlated,
        };

        var rotorInertia = new ProductExpression([half, rotorMass, radiusSquared]) { Id = "I" };

        // ─── spin angular momentum, then precession: Ω_p = τ·rad / (I·ω_s) ────────────────────────────────
        var spinAngularMomentum = new ProductExpression([rotorInertia, spinRate]) { Id = "L_s" };

        var precessionRate = new QuotientExpression
        {
            Id = PrecessionRateId,
            Numerator = new ProductExpression([appliedTorque, radian]) { Id = "τ·rad" },
            Denominator = spinAngularMomentum,
        };

        var precessionPeriod = new QuotientExpression
        {
            Id = "T_p",
            Numerator = revolution,
            Denominator = precessionRate,
        };

        var validityLimit = new ProductExpression([validityFraction, spinRate]) { Id = "Ω_max" };

        // ─── the model ────────────────────────────────────────────────────────────────────────────────────
        var system = ExpressionSystem.Create(
            "Gyroscopic precession",
            "Steady precession of a gimbal-mounted flywheel under a constant applied torque.");

        system.Add(precessionPeriod);

        // A computed rate against a timed one: the two reach the same quantity by genuinely different routes,
        // so agreement here says something about the data and not merely about the tree.
        system.Add(new EqualityOperator(AgreementRule.Mutual, SolvingRole.Coherence)
        {
            Id = "computed vs timed precession",
            Lhs = precessionRate,
            Rhs = timedPrecession,
        });

        // A requirement on the model rather than on the design: it asks whether the formula used to produce
        // the answer applies to the answer it produced.
        system.Add(new DefinitelyLessThanOperator
        {
            Id = "slow-precession assumption holds",
            Lhs = precessionRate,
            Rhs = validityLimit,
        });

        return new WorkedExample(
            "Gyroscopic precession of a flywheel",
            "mechanical engineering",
            "A steel flywheel spins at 3600 rpm in a gimbal. A steady torque is applied about an axis "
            + "perpendicular to the spin axis, and the rotor responds by precessing about the third axis. "
            + "Find the precession rate and period, confirm them against a stopwatch, and check that the "
            + "precession is slow enough for the steady formula to apply.",
            system,
            [
                new Readout("rotor mass m", rotorMass, Mass.Kilogram),
                new Readout("rotor radius r", rotorRadius, Length.Meter),
                new Readout("spin rate ω_s", spinRate, AngularVelocity.Rpm),
                new Readout("applied torque τ", appliedTorque, Torque.NewtonMeter),
                new Readout("timed precession", timedPrecession, AngularVelocity.DegreePerSecond),
                new Readout("validity limit", validityFraction, Dimensionless.One, "of the spin rate"),
            ],
            [
                new Readout("rotor inertia I", rotorInertia, MomentOfInertia.KilogramMeterSquared),
                new Readout("spin momentum L_s", spinAngularMomentum, AngularMomentum.KilogramMeterSquaredPerSecond),
                new Readout("precession rate Ω_p", precessionRate, AngularVelocity.Rpm),
                new Readout("precession rate Ω_p", precessionRate, AngularVelocity.DegreePerSecond),
                new Readout("precession period T_p", precessionPeriod, Time.Second),
                new Readout("validity limit Ω_max", validityLimit, AngularVelocity.Rpm),
            ]);
    }
}
