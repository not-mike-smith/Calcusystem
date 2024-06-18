using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;

namespace Measurement.Units;

public class Jerk : ReflectiveUnitList<Jerk>
{
    private Jerk() { }
    public static readonly Jerk Units = new();

    public static readonly UnitOfMeasure MeterPerSecondCubed = UnitFactory.Create(
        "m/s³",
        (Length.Meter, 1),
        (Time.Second, -3));

    public static readonly UnitOfMeasure FootPerSecondCubed = UnitFactory.Create(
        "ft/s³",
        (Length.Foot, 1),
        (Time.Second, -3));
}
