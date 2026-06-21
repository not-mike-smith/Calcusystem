using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class AngularMomentum : ReflectiveUnitList<AngularMomentum>
{
    private AngularMomentum() { }
    public static readonly AngularMomentum Units = new();

    public static readonly Uom KilogramMeterSquaredPerSecond = UnitFactory.Create(
        "kg·m²/s",
        (MomentOfInertia.KilogramMeterSquared, 1),
        (AngularVelocity.RadPerSecond, 1));

    public static readonly Uom PoundFootSquaredPerSecond = UnitFactory.Create(
        "lb·ft²/s",
        (MomentOfInertia.PoundFootSquared, 1),
        (AngularVelocity.RadPerSecond, 1));
}
