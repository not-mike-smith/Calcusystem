using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

// Magnetic flux (M·L²·t⁻²·A⁻¹ = Wb = V·s).
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class MagneticFlux : ReflectiveUnitList<MagneticFlux>
{
    private MagneticFlux() { }
    public static readonly MagneticFlux Units = new();

    // SI
    public static readonly Uom Weber = UnitFactory.Create(
        "Wb",
        (ElectricPotential.Volt, 1),
        (Time.Second, 1));

    public static readonly Uom Milliweber = Metric.m(Weber);
    public static readonly Uom Microweber = Metric.micro(Weber);

    // CGS (1 Maxwell = 1e-8 Wb)
    public static readonly Uom Maxwell = UnitFactory.Create("Mx", 1e-8, Weber);
}
