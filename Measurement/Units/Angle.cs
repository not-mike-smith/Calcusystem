using System;
using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;

namespace Measurement.Units;

public class Angle : ReflectiveUnitList<Angle>
{
    private Angle() { }
    public static readonly Angle Units = new ();
    public static UnitOfMeasure Radian = UnitFactory.Create("rad", Dimensionality.Angle, 1);
    public static UnitOfMeasure Milliradian = Metric.m(Radian);
    public static UnitOfMeasure Revolution = UnitFactory.Create("rev", 2 * Math.PI, Radian);
    public static UnitOfMeasure Degree = UnitFactory.Create("°", Math.PI / 180, Radian);
    public static UnitOfMeasure ArcMinute = UnitFactory.Create("′", 60, Degree);
    public static UnitOfMeasure ArcSecond = UnitFactory.Create("″", 60, ArcMinute);
    public static UnitOfMeasure Gradian = UnitFactory.Create("grad", 400, Revolution);
}
