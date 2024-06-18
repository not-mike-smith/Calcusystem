using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;

namespace Measurement.Units;

public class Acceleration : ReflectiveUnitList<Acceleration>
{
    private Acceleration() { }
    public static readonly Acceleration Units = new();

    public static readonly UnitOfMeasure MeterPerSecondSquared = UnitFactory.Create(
        "m/s²",
        (Length.Meter, 1),
        (Time.Second, -2));

    public static readonly UnitOfMeasure FootPerSecondSquared = UnitFactory.Create(
        "ft/s²",
        (Length.Foot, 1),
        (Time.Second, -2));
}
