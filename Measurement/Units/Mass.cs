using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;

namespace Measurement.Units;

public class Mass : ReflectiveUnitList<Mass>
{
    private Mass() { }
    public static readonly Mass Units = new();

    public static readonly UnitOfMeasure Kilogram  = UnitFactory.Create("kg", Dimensionality.Mass, 1);
    public static readonly UnitOfMeasure Gram  = UnitFactory.Create("g", 0.001, Kilogram);
    public static readonly UnitOfMeasure Milligram = Metric.m(Gram);
    public static readonly UnitOfMeasure Microgram = Metric.micro(Gram);
    public static readonly UnitOfMeasure Nanogram = Metric.n(Gram);
    public static readonly UnitOfMeasure Picogram = Metric.p(Gram);
    public static readonly UnitOfMeasure Femtogram = Metric.f(Gram);
    public static readonly UnitOfMeasure Attogram = Metric.a(Gram);
    public static readonly UnitOfMeasure Zeptogram = Metric.z(Gram);
    public static readonly UnitOfMeasure Yoctogram = Metric.y(Gram);
    public static readonly UnitOfMeasure Tonne  = UnitFactory.Create("tonne", 1000, Kilogram);
    public static readonly UnitOfMeasure Kilotonne = Metric.k(Tonne);
    public static readonly UnitOfMeasure Megatonne = Metric.M(Tonne);
    public static readonly UnitOfMeasure Gigatonne = Metric.G(Tonne);
    public static readonly UnitOfMeasure Ounce  = UnitFactory.Create("oz", 28.349523, Gram);
    public static readonly UnitOfMeasure Pound  = UnitFactory.Create("lb", 16, Ounce);
    public static readonly UnitOfMeasure Ton  = UnitFactory.Create("ton", 2000, Pound);
    public static readonly UnitOfMeasure Grain  = UnitFactory.Create("gr", 0.064799, Gram);
    public static readonly UnitOfMeasure TroyOunce  = UnitFactory.Create("oz t", 480, Grain);
}
