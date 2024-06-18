using System;
using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class Length : ReflectiveUnitList<Length>
{
    private Length() { }
    public static readonly Length Units = new();

    public static readonly Uom Meter = UnitFactory.Create("m", Dimensionality.Length, 1);
    public static readonly Uom Kilometer = Metric.k(Meter);
    public static readonly Uom Megameter = Metric.M(Meter);
    public static readonly Uom Decimeter = Metric.d(Meter);
    public static readonly Uom Centimeter = Metric.c(Meter);
    public static readonly Uom Millimeter = Metric.m(Meter);
    public static readonly Uom Micron = Metric.micro(Meter);
    public static readonly Uom Nanometer = Metric.n(Meter);
    public static readonly Uom Picometer = Metric.p(Meter);
    public static readonly Uom Femtometer = Metric.f(Meter);
    public static readonly Uom Attometer = Metric.a(Meter);
    public static readonly Uom Zeptometer = Metric.z(Meter);
    public static readonly Uom Yoctometer = Metric.y(Meter);

    // Imperial
    public static readonly Uom Foot = UnitFactory.Create("ft", 0.3048, Meter);
    public static readonly Uom Inch = UnitFactory.Create("in", 1d/12, Foot);
    public static readonly Uom Thou = UnitFactory.Create("thou", 0.001, Inch);
    public static readonly Uom Yard = UnitFactory.Create("yd", 3, Foot);
    public static readonly Uom Mile = UnitFactory.Create("mi", 5280, Foot);

    // Atomic
    public static readonly Uom Angstrom = UnitFactory.Create("Å", 1E10, Meter);
    public static readonly Uom XUnit = UnitFactory.Create("xu", 1d/1.0021E-13, Meter);
    public static readonly Uom PlanckLength = UnitFactory.Create("ℓₚ", 1d / 1.61625518E-35, Meter);
    public static readonly Uom BohrRadius = UnitFactory.Create("a₀", 1d / 5.2917721090380E-11, Meter);

    // Astronomical
    public static readonly Uom AstronomicalUnit = UnitFactory.Create("au", 149597870700, Meter);
    public static readonly Uom LightYear = UnitFactory.Create("ly", 9460730472580800, Meter);
    public static readonly Uom Parsec = UnitFactory.Create("pc", 180d * 60 * 60 / Math.PI, AstronomicalUnit);

    // Nautical
    public static readonly Uom NauticalMile = UnitFactory.Create("NM", 1852, Meter);
    public static readonly Uom Cable = UnitFactory.Create("cable", 0.1, NauticalMile);
    public static readonly Uom Fathom = UnitFactory.Create("fathom", 2, Yard);

    // Surveying
    public static readonly Uom Chain = UnitFactory.Create("ch", 22, Yard);
    public static readonly Uom Rod = UnitFactory.Create("rod", 16.5, Foot);
}
