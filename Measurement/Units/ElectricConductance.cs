using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

// Electric conductance (A²·t³·M⁻¹·L⁻² = S = A/V = 1/Ω).
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class ElectricConductance : ReflectiveUnitList<ElectricConductance>
{
    private ElectricConductance() { }
    public static readonly ElectricConductance Units = new();

    public static readonly Uom Siemens = UnitFactory.Create(
        "S",
        (ElectricCurrent.Ampere, 1),
        (ElectricPotential.Volt, -1));

    public static readonly Uom Millisiemens = Metric.m(Siemens);
    public static readonly Uom Microsiemens = Metric.micro(Siemens);
    public static readonly Uom Nanosiemens = Metric.n(Siemens);

    // Legacy name for siemens, still common in older literature
    public static readonly Uom Mho = UnitFactory.Create("℧", 1, Siemens);
}
