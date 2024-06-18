using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class Area : ReflectiveUnitList<Area>
{
    private Area() { }
    public static readonly Area Units = new();

    public static readonly Uom SquareMeter = UnitFactory.Create("m²", (Length.Meter, 2));
    public static readonly Uom SquareFoot = UnitFactory.Create("ft²", (Length.Foot, 2));
    public static readonly Uom SquareInch = UnitFactory.Create("in²", (Length.Inch, 2));
    public static readonly Uom SquareCm = UnitFactory.Create("cm²", (Length.Centimeter, 2));
    public static readonly Uom SquareMm = UnitFactory.Create("mm²", (Length.Millimeter, 2));
    public static readonly Uom SquareKm = UnitFactory.Create("km²", (Length.Kilometer, 2));
    public static readonly Uom SquareMile = UnitFactory.Create("mi²", (Length.Mile, 2));

    public static readonly Uom Barn = UnitFactory.Create("barn", 1E-28, SquareMeter);

    public static readonly Uom Hectare = UnitFactory.Create("ha", 1e4, SquareMeter);
    public static readonly Uom Acre = UnitFactory.Create("acre", 43560, SquareFoot);
}
