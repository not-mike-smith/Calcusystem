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
/// Chemical engineering: a tank draining to atmosphere through a sharp-edged orifice in its floor.
/// </summary>
/// <remarks>
/// The classic case is a differential equation — head falls, so discharge falls with it. For a prismatic tank
/// that equation integrates in closed form, and it is the closed form that is modeled here. Nothing in this
/// platform integrates anything.
/// </remarks>
public static class OrificeDrainTime
{
    /// <summary>The id of the discharge coefficient, the one value worth withholding to see what stalls.</summary>
    public const string DischargeCoefficientId = "C_d";

    /// <summary>The id of the computed time to empty.</summary>
    public const string DrainTimeId = "t_drain";

    /// <summary>The id of the computed initial discharge.</summary>
    public const string InitialDischargeId = "Q₀";

    /// <summary>Builds the model, its two corroborating checks, and its two design requirements.</summary>
    public static WorkedExample Build()
    {
        // ─── given ────────────────────────────────────────────────────────────────────────────────────────
        var head = Inputs.Known(
            "h₀", 3.200, Length.Meter, 0.010.Units(Length.Meter),
            ProvenanceFactory.Measured("LT-204", new DateOnly(2026, 3, 14)));

        var tankDiameter = Inputs.Known(
            "D", 2.400, Length.Meter, 0.005.Units(Length.Meter),
            ProvenanceFactory.Design("TK-101 rev C"));

        var orificeDiameter = Inputs.Known(
            "d₀", 50.00, Length.Millimeter, 0.10.Units(Length.Millimeter),
            ProvenanceFactory.Design("TK-101 rev C, detail 4"));

        var dischargeCoefficient = Inputs.Known(
            DischargeCoefficientId, 0.610, Dimensionless.One, 2.0.Percent(),
            ProvenanceFactory.Model("sharp-edged orifice, turbulent", "Perry's 8th ed., table 10-9"));

        var gravity = Inputs.Exact(
            "g", 9.80665, Acceleration.MeterPerSecondSquared,
            ProvenanceFactory.Reference("CGPM 1901, standard acceleration of free fall", year: 1901));

        var meteredFlow = Inputs.Known(
            "Q_meter", 152.0, VolumetricFlow.GallonPerMinute, 3.0.Percent(),
            ProvenanceFactory.Measured("FT-311", new DateOnly(2026, 1, 9)));

        // Limits are decisions, so they are exact. The tolerance belongs to the thing being limited.
        var allowedTime = Inputs.Exact(
            "t_allowed", 60, Time.Minute, ProvenanceFactory.Design("batch schedule, step 7"));

        var headerCapacity = Inputs.Exact(
            "Q_header", 160.0, VolumetricFlow.GallonPerMinute, ProvenanceFactory.Design("line 6\"-WD-204"));

        var quarterPi = Inputs.Exact("π/4", Math.PI / 4, Dimensionless.One);
        var two = Inputs.Exact("2", 2, Dimensionless.One);

        // ─── areas ────────────────────────────────────────────────────────────────────────────────────────
        // A diameter listed twice is squared, and the two listings are the same measurement — so the node is
        // Correlated, which sums the relative uncertainties instead of combining them in quadrature. Treating
        // them as independent would report 0.29 % where the right answer is 0.42 %.
        var tankDiameterSquared = new ProductExpression([tankDiameter, tankDiameter])
        {
            Id = "D²",
            UncertaintyCorrelation = UncertaintyCorrelation.Correlated,
        };

        var orificeDiameterSquared = new ProductExpression([orificeDiameter, orificeDiameter])
        {
            Id = "d₀²",
            UncertaintyCorrelation = UncertaintyCorrelation.Correlated,
        };

        // π/4 is exact, so this node has one uncertain factor and the default Uncorrelated is right. Gathering
        // the square into its own node above is what lets each node state its own correlation.
        var tankArea = new ProductExpression([quarterPi, tankDiameterSquared]) { Id = "A_tank" };
        var orificeArea = new ProductExpression([quarterPi, orificeDiameterSquared]) { Id = "A₀" };
        var tankVolume = new ProductExpression([tankArea, head]) { Id = "V_tank" };

        // ─── initial discharge: Q₀ = C_d·A₀·√(2gh₀) ───────────────────────────────────────────────────────
        var velocityHead = new ProductExpression([two, gravity, head]) { Id = "2gh₀" };
        var jetVelocity = new SqrtExpression(velocityHead, "v_jet");

        var initialDischarge = new ProductExpression([dischargeCoefficient, orificeArea, jetVelocity])
        {
            Id = InitialDischargeId,
        };

        // ─── time to empty: t = (A_tank / C_d·A₀)·√(2h₀/g) ────────────────────────────────────────────────
        var fallTime = new SqrtExpression(
            new QuotientExpression
            {
                Id = "2h₀/g",
                Numerator = new ProductExpression([two, head]) { Id = "2h₀" },
                Denominator = gravity,
            },
            "√(2h₀/g)");

        var drainTime = new QuotientExpression
        {
            Id = DrainTimeId,
            Numerator = new ProductExpression([tankArea, fallTime]) { Id = "A_tank·√(2h₀/g)" },
            Denominator = new ProductExpression([dischargeCoefficient, orificeArea]) { Id = "C_d·A₀" },
        };

        // Mean discharge, reached two ways: emptied volume over elapsed time, and half the initial rate.
        var meanDischarge = new QuotientExpression
        {
            Id = "V_tank/t_drain",
            Numerator = tankVolume,
            Denominator = drainTime,
        };

        var halfInitialDischarge = new QuotientExpression
        {
            Id = "Q₀/2",
            Numerator = initialDischarge,
            Denominator = two,
        };

        // ─── the model ────────────────────────────────────────────────────────────────────────────────────
        var system = ExpressionSystem.Create(
            "Orifice drain time",
            "Torricelli discharge and the integrated drain time for a prismatic tank.");

        system.Add(drainTime);
        system.Add(meanDischarge);

        // An identity, algebraically: both sides reduce to C_d·A₀·√(gh₀/2). So it corroborates nothing about
        // the data and everything about the tree — a factor dropped or a quotient inverted shows up here.
        system.Add(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Coherence)
        {
            Id = "mean discharge, two routes",
            Lhs = meanDischarge,
            Rhs = halfInitialDischarge,
        });

        // This one does corroborate the data: a computed rate against a metered one. Mutual agreement is the
        // usual reading of "two independent measurements agree" — each reported value inside the other's band.
        system.Add(new EqualityOperator(AgreementRule.Mutual, SolvingRole.Coherence)
        {
            Id = "computed vs metered flow",
            Lhs = initialDischarge,
            Rhs = meteredFlow,
        });

        system.Add(new DefinitelyLessThanOperator
        {
            Id = "empties inside the window",
            Lhs = drainTime,
            Rhs = allowedTime,
        });

        system.Add(new DefinitelyLessThanOperator
        {
            Id = "header can take the peak",
            Lhs = initialDischarge,
            Rhs = headerCapacity,
        });

        return new WorkedExample(
            "Tank draining through an orifice",
            "chemical engineering",
            "A vertical cylindrical tank of water, open to atmosphere, drains through a sharp-edged orifice "
            + "in its floor. Find the initial discharge and the time to empty, confirm the computed rate "
            + "against the flow meter, and show that the batch window and the header capacity both hold.",
            system,
            [
                new Readout("initial head h₀", head, Length.Meter),
                new Readout("tank diameter D", tankDiameter, Length.Meter),
                new Readout("orifice diameter d₀", orificeDiameter, Length.Millimeter),
                new Readout("discharge coeff. C_d", dischargeCoefficient, Dimensionless.One),
                new Readout("gravity g", gravity, Acceleration.MeterPerSecondSquared),
                new Readout("metered flow", meteredFlow, VolumetricFlow.GallonPerMinute),
                new Readout("batch window", allowedTime, Time.Minute),
                new Readout("header capacity", headerCapacity, VolumetricFlow.GallonPerMinute),
            ],
            [
                new Readout("orifice area A₀", orificeArea, Area.SquareMeter),
                new Readout("tank area A_tank", tankArea, Area.SquareMeter),
                new Readout("tank volume V_tank", tankVolume, Volume.CubicMeter),
                new Readout("jet velocity v_jet", jetVelocity, Speed.MeterPerSecond),
                new Readout("initial discharge Q₀", initialDischarge, VolumetricFlow.GallonPerMinute),
                new Readout("time to empty", drainTime, Time.Minute),
                new Readout("mean discharge, V/t", meanDischarge, VolumetricFlow.GallonPerMinute),
                new Readout("mean discharge, Q₀/2", halfInitialDischarge, VolumetricFlow.GallonPerMinute),
            ]);
    }
}
