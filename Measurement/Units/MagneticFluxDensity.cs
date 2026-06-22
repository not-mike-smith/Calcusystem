using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

// Magnetic flux density / field strength (M·t⁻²·A⁻¹ = T = kg/(A·s²) = Wb/m²).
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class MagneticFluxDensity : ReflectiveUnitList<MagneticFluxDensity>
{
    private MagneticFluxDensity() { }
    public static readonly MagneticFluxDensity Units = new();

    // SI
    public static readonly Uom Tesla = UnitFactory.Create(
        "T",
        (Mass.Kilogram, 1),
        (Time.Second, -2),
        (ElectricCurrent.Ampere, -1));

    public static readonly Uom Millitesla = Metric.m(Tesla);
    public static readonly Uom Microtesla = Metric.micro(Tesla);
    public static readonly Uom Nanotesla = Metric.n(Tesla); // Earth's field varies by ~10s of nT

    // CGS (1 Gauss = 1e-4 T)
    public static readonly Uom Gauss = UnitFactory.Create("G", 1e-4, Tesla);
    public static readonly Uom Milligauss = Metric.m(Gauss);
}
