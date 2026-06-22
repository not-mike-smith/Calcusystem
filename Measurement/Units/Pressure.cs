using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class Pressure : ReflectiveUnitList<Pressure>
{
    private Pressure() { }
    public static readonly Pressure Units = new();

    private const double StandardAtmospherePa = 101325.0;

    // SI
    public static readonly Uom Pascal = UnitFactory.Create("Pa", (Force.Newton, 1), (Area.SquareMeter, -1));
    public static readonly Uom Kilopascal = Metric.k(Pascal);
    public static readonly Uom Megapascal = Metric.M(Pascal);
    public static readonly Uom Gigapascal = Metric.G(Pascal);
    public static readonly Uom Millibar = UnitFactory.Create("mbar", 100, Pascal);
    public static readonly Uom Bar = UnitFactory.Create("bar", 100000, Pascal);

    // Absolute atmosphere references
    public static readonly Uom Atmosphere = UnitFactory.Create("atm", StandardAtmospherePa, Pascal);

    // Hg / H2O column
    public static readonly Uom MillimeterOfMercury = UnitFactory.Create("mmHg", StandardAtmospherePa / 760, Pascal);
    public static readonly Uom Torr = UnitFactory.Create("Torr", StandardAtmospherePa / 760, Pascal);
    public static readonly Uom InchOfMercury = UnitFactory.Create("inHg", 25.4, MillimeterOfMercury);
    public static readonly Uom InchOfWater = UnitFactory.Create("inH₂O", 249.0889, Pascal);

    // Imperial
    public static readonly Uom PoundPerSquareInch = UnitFactory.Create("psi", (Force.PoundForce, 1), (Area.SquareInch, -1));
    public static readonly Uom KilopoundPerSquareInch = UnitFactory.Create("ksi", 1000, PoundPerSquareInch);

    // Nominal gauge (offset = 1 standard atmosphere; true gauge requires an expression relationship)
    public static readonly Models.OffsetUnitOfMeasure BarGauge = UnitFactory.Create(
        "barg", Bar.KmsConversionFactor, Bar, StandardAtmospherePa / Bar.KmsConversionFactor);
    public static readonly Models.OffsetUnitOfMeasure KilopascalGauge = UnitFactory.Create(
        "kPag", Kilopascal.KmsConversionFactor, Kilopascal, StandardAtmospherePa / Kilopascal.KmsConversionFactor);
    public static readonly Models.OffsetUnitOfMeasure PsiGauge = UnitFactory.Create(
        "psig", PoundPerSquareInch.KmsConversionFactor, PoundPerSquareInch, StandardAtmospherePa / PoundPerSquareInch.KmsConversionFactor);
}
