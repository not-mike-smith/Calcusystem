using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class Volume : ReflectiveUnitList<Volume>
{
    private Volume() { }
    public static readonly Volume Units = new();

    public static readonly Uom CubicMeter = UnitFactory.Create("m³", (Length.Meter, 3));
    public static readonly Uom CubicFoot = UnitFactory.Create("ft³", (Length.Foot, 3));
    public static readonly Uom CubicInch = UnitFactory.Create("in³", (Length.Inch, 3));
    public static readonly Uom CubicCentimeter = UnitFactory.Create("cc", (Length.Centimeter, 3));
    public static readonly Uom Milliliter = UnitFactory.Create("mL", 1, CubicCentimeter);
    public static readonly Uom Liter = UnitFactory.Create("L", 1000, Milliliter);

    public static readonly Uom Gallon = UnitFactory.Create("gal", 231, CubicInch);
    public static readonly Uom Quart = UnitFactory.Create("qt", 0.25, Gallon);
    public static readonly Uom Pint = UnitFactory.Create("pt", 0.5, Quart);
    public static readonly Uom Cup = UnitFactory.Create("cup", 0.5, Pint);
    public static readonly Uom Tablespoon = UnitFactory.Create("tbsp", 1d / 16, Cup);
    public static readonly Uom Teaspoon = UnitFactory.Create("tsp", 1d / 48, Cup);
    public static readonly Uom FluidOunce = UnitFactory.Create("fl oz", 0.125, Cup);

    public static readonly Uom ImperialGallon = UnitFactory.Create("imp gal", 4.54609, Liter);
    public static readonly Uom ImperialQuart = UnitFactory.Create("imp qt", 0.25, ImperialGallon);
    public static readonly Uom ImperialPint = UnitFactory.Create("imp pt", 0.5, ImperialQuart);
    public static readonly Uom ImperialCup = UnitFactory.Create("imp cup", 0.5, ImperialPint);
    public static readonly Uom ImperialFluidOunce = UnitFactory.Create("imp fl oz", 0.125, ImperialCup);

    public static readonly Uom MetricCup = UnitFactory.Create("m cup", 250, Milliliter);
}
