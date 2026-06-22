using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

// Linear momentum (M·L·t⁻¹) and impulse share the same dimensions.
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class Momentum : ReflectiveUnitList<Momentum>
{
    private Momentum() { }
    public static readonly Momentum Units = new();

    // SI
    public static readonly Uom KilogramMeterPerSecond = UnitFactory.Create(
        "kg·m/s",
        (Mass.Kilogram, 1),
        (Speed.MeterPerSecond, 1));

    public static readonly Uom NewtonSecond = UnitFactory.Create(
        "N·s",
        (Force.Newton, 1),
        (Time.Second, 1));

    // English Engineering
    public static readonly Uom SlugFootPerSecond = UnitFactory.Create(
        "slug·ft/s",
        (Mass.Slug, 1),
        (Speed.FootPerSecond, 1));

    public static readonly Uom PoundForceSecond = UnitFactory.Create(
        "lbf·s",
        (Force.PoundForce, 1),
        (Time.Second, 1));
}
