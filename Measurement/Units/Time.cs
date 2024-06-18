using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class Time : ReflectiveUnitList<Time>
{
    private Time() { }
    public static Time Units = new();

    public static readonly Uom Second = UnitFactory.Create("s", Dimensionality.Time, 1);
    public static readonly Uom Minute = UnitFactory.Create("min", 60, Second);
    public static readonly Uom Hour = UnitFactory.Create("h", 60, Minute);
    public static readonly Uom Day = UnitFactory.Create("day", 24, Hour);
    public static readonly Uom Week = UnitFactory.Create("wk", 7, Day);
    public static readonly Uom Fortnight = UnitFactory.Create("fortnight", 2, Week);
    public static readonly Uom GregorianYear = UnitFactory.Create("yr", 365.2425, Day);
    public static readonly Uom JulianYear = UnitFactory.Create("Jyr", 365.25, Day);
    public static readonly Uom CommonYear = UnitFactory.Create("common-yr", 365, Day);

    public static readonly Uom Millisecond = Metric.m(Second);
    public static readonly Uom Microsecond = Metric.micro(Second);
    public static readonly Uom Nanosecond = Metric.n(Second);
    public static readonly Uom Picosecond = Metric.p(Second);
    public static readonly Uom Femtosecond = Metric.f(Second);
    public static readonly Uom Attosecond = Metric.a(Second);
    public static readonly Uom Zeptosecond = Metric.z(Second);
    public static readonly Uom Yoctosecond = Metric.y(Second);

    public static readonly Uom PlankTime = UnitFactory.Create("𝘵ₚ", 5.39056E-44, Second);
    public static readonly Uom Jiffy = UnitFactory.Create("jiffy", 3E-24, Second);
    public static readonly Uom Svedberg = UnitFactory.Create("Sv", 1E-13, Second);
    public static readonly Uom Shake = UnitFactory.Create("shake", 1E-8, Second);
}
