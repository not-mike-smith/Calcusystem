using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

// Electric capacitance (A²·s⁴·M⁻¹·L⁻² = F = C/V).
// Practical capacitors span millifarads down to femtofarads; the base Farad is rarely encountered.
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class ElectricCapacitance : ReflectiveUnitList<ElectricCapacitance>
{
    private ElectricCapacitance() { }
    public static readonly ElectricCapacitance Units = new();

    public static readonly Uom Farad = UnitFactory.Create(
        "F",
        (ElectricCharge.Coulomb, 1),
        (ElectricPotential.Volt, -1));

    public static readonly Uom Millifarad = Metric.m(Farad);
    public static readonly Uom Microfarad = Metric.micro(Farad);
    public static readonly Uom Nanofarad = Metric.n(Farad);
    public static readonly Uom Picofarad = Metric.p(Farad);
    public static readonly Uom Femtofarad = Metric.f(Farad);
}
