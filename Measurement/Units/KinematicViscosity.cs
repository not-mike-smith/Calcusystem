using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

// Kinematic viscosity (L²·t⁻¹ = m²/s = dynamic viscosity / density).
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class KinematicViscosity : ReflectiveUnitList<KinematicViscosity>
{
    private KinematicViscosity() { }
    public static readonly KinematicViscosity Units = new();

    // SI
    public static readonly Uom SquareMeterPerSecond = UnitFactory.Create(
        "m²/s",
        (Area.SquareMeter, 1),
        (Time.Second, -1));

    // 1 mm²/s = 1 cSt (water at 20°C ≈ 1 cSt)
    public static readonly Uom SquareMillimeterPerSecond = UnitFactory.Create(
        "mm²/s",
        (Area.SquareMm, 1),
        (Time.Second, -1));

    // CGS
    public static readonly Uom Stoke = UnitFactory.Create("St", 1e-4, SquareMeterPerSecond);
    public static readonly Uom Centistoke = Metric.c(Stoke); // = 1 mm²/s = 1e-6 m²/s
}
