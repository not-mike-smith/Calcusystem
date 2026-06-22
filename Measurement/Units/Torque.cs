using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

// Torque (M·L²·Θ·t⁻²) — dimensionally distinct from energy (M·L²·t⁻²) because
// torque is the rate of change of angular momentum (which carries the angle dimension).
// The Radian factor in each definition is what separates torque from energy in this system.
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class Torque : ReflectiveUnitList<Torque>
{
    private Torque() { }
    public static readonly Torque Units = new();

    // SI
    public static readonly Uom NewtonMeter = UnitFactory.Create(
        "N·m",
        (Force.Newton, 1),
        (Length.Meter, 1),
        (Angle.Radian, 1));

    public static readonly Uom KilonewtonMeter = Metric.k(NewtonMeter);
    public static readonly Uom MillinewtonMeter = Metric.m(NewtonMeter);

    // English Engineering
    public static readonly Uom PoundForceFoot = UnitFactory.Create(
        "lbf·ft",
        (Force.PoundForce, 1),
        (Length.Foot, 1),
        (Angle.Radian, 1));

    public static readonly Uom PoundForceInch = UnitFactory.Create(
        "lbf·in",
        (Force.PoundForce, 1),
        (Length.Inch, 1),
        (Angle.Radian, 1));

    // Common in small motors and electronics
    public static readonly Uom OunceForceInch = UnitFactory.Create(
        "ozf·in",
        (Force.OunceForce, 1),
        (Length.Inch, 1),
        (Angle.Radian, 1));
}
