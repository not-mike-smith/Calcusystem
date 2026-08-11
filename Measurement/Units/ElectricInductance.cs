using Calcusystem.Measurement.BaseClasses;
using Calcusystem.Measurement.Factories;
using Uom = Calcusystem.Measurement.UnitOfMeasure;

namespace Calcusystem.Measurement.Units;

// Electric inductance (M·L²·I⁻²·T⁻² = H = V·s/A).
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class ElectricInductance : ReflectiveUnitList<ElectricInductance>
{
    private ElectricInductance() { }
    public static readonly ElectricInductance Units = new();

    public static readonly Uom Henry = UnitFactory.Create(
        "H",
        (ElectricPotential.Volt, 1),
        (Time.Second, 1),
        (ElectricCurrent.Ampere, -1));

    public static readonly Uom Millihenry = Metric.m(Henry);
    public static readonly Uom Microhenry = Metric.micro(Henry);
    public static readonly Uom Nanohenry = Metric.n(Henry);
}
