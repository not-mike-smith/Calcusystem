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
/// Electrical engineering: three resistive loads across a DC bus, and whether the supply breaker holds.
/// </summary>
/// <remarks>
/// Resistive and DC on purpose. An LRC circuit needs either a differential equation or complex impedance, and
/// this platform has neither — a <c>Measurand</c> is a real number with an uncertainty on it.
/// </remarks>
public static class ParallelDcLoads
{
    /// <summary>The id of the computed total supply current — the one the breaker sees.</summary>
    public const string SupplyCurrentId = "I_total";

    /// <summary>The id of the breaker requirement, which this model does not satisfy.</summary>
    public const string BreakerCheckId = "supply breaker holds";

    /// <summary>Builds the model, the Kirchhoff check on it, and the two protective-device requirements.</summary>
    public static WorkedExample Build()
    {
        // ─── given ────────────────────────────────────────────────────────────────────────────────────────
        var busVoltage = Inputs.Known(
            "V_bus", 24.00, ElectricPotential.Volt, 0.5.Percent(),
            ProvenanceFactory.Measured("VT-01", new DateOnly(2026, 2, 2)));

        var lighting = Inputs.Known(
            "R₁", 12.00, ElectricResistance.Ohm, 1.0.Percent(),
            ProvenanceFactory.Design("cabin lighting loop"));

        var instruments = Inputs.Known(
            "R₂", 8.200, ElectricResistance.Ohm, 1.0.Percent(),
            ProvenanceFactory.Design("instrument rack"));

        var heater = Inputs.Known(
            "R₃", 47.00, ElectricResistance.Ohm, 5.0.Percent(),
            ProvenanceFactory.Design("trace heater, added under ECO-88"));

        var breakerRating = Inputs.Exact(
            "I_breaker", 5.00, ElectricCurrent.Ampere, ProvenanceFactory.Design("CB-1, thermal-magnetic"));

        var instrumentFuse = Inputs.Exact(
            "I_fuse2", 3.00, ElectricCurrent.Ampere, ProvenanceFactory.Design("F2, fast-blow"));

        // ─── conductances, because parallel branches add in conductance ───────────────────────────────────
        var lightingConductance = new ReciprocalExpression(lighting, "G₁");
        var instrumentConductance = new ReciprocalExpression(instruments, "G₂");
        var heaterConductance = new ReciprocalExpression(heater, "G₃");

        var totalConductance = new SumExpression(
            [lightingConductance, instrumentConductance, heaterConductance]) { Id = "G_total" };

        var equivalentResistance = new ReciprocalExpression(totalConductance, "R_eq");

        // ─── currents ─────────────────────────────────────────────────────────────────────────────────────
        var lightingCurrent = new ProductExpression([busVoltage, lightingConductance]) { Id = "I₁" };
        var instrumentCurrent = new ProductExpression([busVoltage, instrumentConductance]) { Id = "I₂" };
        var heaterCurrent = new ProductExpression([busVoltage, heaterConductance]) { Id = "I₃" };

        var supplyCurrent = new ProductExpression([busVoltage, totalConductance]) { Id = SupplyCurrentId };

        var branchCurrentSum = new SumExpression([lightingCurrent, instrumentCurrent, heaterCurrent])
        {
            Id = "ΣI_branch",
        };

        // ─── power ────────────────────────────────────────────────────────────────────────────────────────
        // P = V²·G, not V·I_total. The two are the same number and not the same uncertainty: I_total already
        // contains V_bus, so multiplying by V_bus again presents one measurement as two independent ones and
        // under-states the result. Squaring V_bus in its own Correlated node says what is actually true — the
        // bus voltage is perfectly correlated with itself, and the conductances are independent of it.
        var busVoltageSquared = new ProductExpression([busVoltage, busVoltage])
        {
            Id = "V_bus²",
            UncertaintyCorrelation = UncertaintyCorrelation.Correlated,
        };

        var totalPower = new ProductExpression([busVoltageSquared, totalConductance]) { Id = "P_total" };

        // ─── the model ────────────────────────────────────────────────────────────────────────────────────
        var system = ExpressionSystem.Create(
            "Parallel DC loads",
            "Three resistive branches on a 24 V bus, checked against their protective devices.");

        system.Add(equivalentResistance);
        system.Add(totalPower);

        // Kirchhoff's current law. Distributivity, so this too is an identity that tests the tree rather than
        // the data — but it is the identity an engineer would check by hand, and it is worth the model saying
        // it holds rather than the reader assuming it.
        system.Add(new EqualityOperator(AgreementRule.Nominal, SolvingRole.Coherence)
        {
            Id = "ΣI_branch ·==· I_total",
            Lhs = branchCurrentSum,
            Rhs = supplyCurrent,
        });

        system.Add(new DefinitelyLessThanOperator
        {
            Id = BreakerCheckId,
            Lhs = supplyCurrent,
            Rhs = breakerRating,
        });

        system.Add(new DefinitelyLessThanOperator
        {
            Id = "instrument fuse holds",
            Lhs = instrumentCurrent,
            Rhs = instrumentFuse,
        });

        return new WorkedExample(
            "Parallel loads on a DC bus",
            "electrical engineering",
            "Three resistive loads sit across a 24 V DC distribution bus: cabin lighting, an instrument rack, "
            + "and a trace heater added later. Find each branch current, the total the supply must carry, and "
            + "the equivalent resistance — then check the total against the 5 A breaker and the instrument "
            + "branch against its 3 A fuse.",
            system,
            [
                new Readout("bus voltage V_bus", busVoltage, ElectricPotential.Volt),
                new Readout("lighting R₁", lighting, ElectricResistance.Ohm),
                new Readout("instruments R₂", instruments, ElectricResistance.Ohm),
                new Readout("trace heater R₃", heater, ElectricResistance.Ohm),
                new Readout("breaker rating", breakerRating, ElectricCurrent.Ampere),
                new Readout("instrument fuse", instrumentFuse, ElectricCurrent.Ampere),
            ],
            [
                new Readout("lighting current I₁", lightingCurrent, ElectricCurrent.Ampere),
                new Readout("instrument current I₂", instrumentCurrent, ElectricCurrent.Ampere),
                new Readout("heater current I₃", heaterCurrent, ElectricCurrent.Ampere),
                new Readout("supply current I_total", supplyCurrent, ElectricCurrent.Ampere),
                new Readout("branch sum ΣI", branchCurrentSum, ElectricCurrent.Ampere),
                new Readout("total conductance", totalConductance, ElectricConductance.Siemens),
                new Readout("equivalent resistance", equivalentResistance, ElectricResistance.Ohm),
                new Readout("total power", totalPower, Power.Watt),
            ]);
    }

    /// <summary>
    /// The same power, written the way that gets its uncertainty wrong — <c>V·I_total</c> rather than
    /// <c>V²·G</c>.
    /// </summary>
    /// <remarks>
    /// Kept so a test can hold the two against each other. It is not in the model and nothing computes it
    /// unless asked.
    /// </remarks>
    public static ProductExpression PowerWrittenWithARepeatedFactor(WorkedExample example) =>
        new([example.Leaf("V_bus"), example.Node(SupplyCurrentId)]) { Id = "V_bus·I_total" };
}
