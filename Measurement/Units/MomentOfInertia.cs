using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class MomentOfInertia : ReflectiveUnitList<MomentOfInertia>
{
    private MomentOfInertia() { }
    public static readonly MomentOfInertia Units = new();

    public static readonly Uom KilogramMeterSquared = UnitFactory.Create("kg·m²", (Mass.Kilogram, 1), (Length.Meter, 2));
    public static readonly Uom GramCentimeterSquared = UnitFactory.Create("g·cm²", (Mass.Gram, 1), (Length.Centimeter, 2));
    public static readonly Uom PoundFootSquared = UnitFactory.Create("lb·ft²", (Mass.Pound, 1), (Length.Foot, 2));
    public static readonly Uom PoundInchSquared = UnitFactory.Create("lb·in²", (Mass.Pound, 1), (Length.Inch, 2));
}
