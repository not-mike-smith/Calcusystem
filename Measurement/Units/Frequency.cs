using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class Frequency : ReflectiveUnitList<Frequency>
{
    private Frequency() { }
    public static readonly Frequency Units = new();

    public static readonly Uom Hertz = UnitFactory.Create(
        "Hz",
        (Time.Second, -1));

    public static readonly Uom Kilohertz = Metric.k(Hertz);
    public static readonly Uom Megahertz = Metric.M(Hertz);
    public static readonly Uom Gigahertz = Metric.G(Hertz);
    public static readonly Uom Terahertz = Metric.T(Hertz);
    public static readonly Uom Petahertz = Metric.P(Hertz);

    public static readonly Uom PerMinute = UnitFactory.Create(
        "min⁻¹",
        (Time.Minute, -1));

    public static readonly Uom PerHour = UnitFactory.Create(
        "h⁻¹",
        (Time.Hour, -1));

    public static readonly Uom PerDay = UnitFactory.Create(
        "day⁻¹",
        (Time.Day, -1));

    public static readonly Uom PerWeek = UnitFactory.Create(
        "wk⁻¹",
        (Time.Week, -1));

    public static readonly Uom PerYear = UnitFactory.Create(
        "yr⁻¹",
        (Time.GregorianYear, -1));

    // Radiation
    public static readonly Uom Becquerel = UnitFactory.Create("Bq", 1, Hertz);
    public static readonly Uom Rutherford = UnitFactory.Create("Rd", 1e6, Becquerel);
    public static readonly Uom Curie = UnitFactory.Create("Ci", 37000, Rutherford);
}
