using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;

namespace Measurement.Units;

public class Power : ReflectiveUnitList<Power>
{
    public static readonly Power Units = new();
    private Power() { }

    public static readonly UnitOfMeasure Watt = UnitFactory.Create(
        "W",
        (Energy.Joule, 1),
        (Time.Second, 1));

    public static readonly UnitOfMeasure Milliwatt = Metric.m(Watt);
    public static readonly UnitOfMeasure Microwatt = Metric.micro(Watt);
    public static readonly UnitOfMeasure Nanowatt = Metric.n(Watt);
    public static readonly UnitOfMeasure Picowatt = Metric.p(Watt);
    public static readonly UnitOfMeasure Femtowatt = Metric.f(Watt);
    public static readonly UnitOfMeasure Attowatt = Metric.a(Watt);
    public static readonly UnitOfMeasure Zeptowatt = Metric.z(Watt);
    public static readonly UnitOfMeasure Yoctowatt = Metric.y(Watt);

    public static readonly UnitOfMeasure Kilowatt = Metric.k(Watt);
    public static readonly UnitOfMeasure Megawatt = Metric.M(Watt);
    public static readonly UnitOfMeasure Gigawatt = Metric.G(Watt);
    public static readonly UnitOfMeasure Terawatt = Metric.T(Watt);
    public static readonly UnitOfMeasure Petawatt = Metric.P(Watt);
    public static readonly UnitOfMeasure Exawatt = Metric.E(Watt);
    public static readonly UnitOfMeasure Zettawatt = Metric.Z(Watt);
    public static readonly UnitOfMeasure Yottawatt = Metric.Y(Watt);

    // horsepower (https://physics.nist.gov/cuu/pdf/sp811.pdf pg. 50)
    public static readonly UnitOfMeasure Horsepower = UnitFactory.Create("hp", 745.6999, Watt);
    public static readonly UnitOfMeasure MetricHorsepower = UnitFactory.Create("hp(metric)", 735.4988, Watt);
    public static readonly UnitOfMeasure ElectricalHorsePower = UnitFactory.Create("hp(electric)", 746, Watt);
    public static readonly UnitOfMeasure BoilerHorsepower = UnitFactory.Create("BHP", 9809.50, Watt);
}
