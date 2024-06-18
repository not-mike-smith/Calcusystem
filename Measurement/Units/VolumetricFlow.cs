using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class VolumetricFlow : ReflectiveUnitList<VolumetricFlow>
{
    private VolumetricFlow() { }
    public static readonly VolumetricFlow Units = new();

    public static readonly Uom CubicMeterPerSecond = UnitFactory.Create(
        (Volume.CubicMeter, 1),
        (Time.Second, -1));

    public static readonly Uom CubicFeetPerSecond = UnitFactory.Create(
        (Volume.CubicFoot, 1),
        (Time.Second, -1));

    public static readonly Uom GallonPerMinute = UnitFactory.Create(
        "gpm",
        (Volume.Gallon, 1),
        (Time.Minute, -1));

    public static readonly Uom MilliliterPerSecond = UnitFactory.Create(
        (Volume.Milliliter, 1),
        (Time.Second, -1));
}
