using Calcusystem.Measurement.BaseClasses;
using Calcusystem.Measurement.Factories;
using Uom = Calcusystem.Measurement.UnitOfMeasure;

namespace Calcusystem.Measurement.Units;

public class AngularVelocity : ReflectiveUnitList<AngularVelocity>
{
    private AngularVelocity() { }
    public static readonly AngularVelocity Units = new();

    public static readonly Uom RadPerSecond = UnitFactory.Create(
        "rad/s",
        (Angle.Radian, 1),
        (Time.Second, -1));

    public static readonly Uom RevolutionPerSecond = UnitFactory.Create(
        "rev/s",
        (Angle.Revolution, 1),
        (Time.Second, -1));

    public static readonly Uom DegreePerSecond = UnitFactory.Create(
        "°/s",
        (Angle.Degree, 1),
        (Time.Second, -1));

    public static readonly Uom Rpm = UnitFactory.Create(
        "rpm",
        (Angle.Revolution, 1),
        (Time.Minute, -1));
}
