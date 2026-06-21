using System;
using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;

namespace Measurement.Units;

public class Angle : ReflectiveUnitList<Angle>
{
    private Angle() { }
    public static readonly Angle Units = new();
    public static readonly UnitOfMeasure Radian = UnitFactory.Create("rad", Dimensionality.Angle, 1);
    public static readonly UnitOfMeasure Milliradian = Metric.m(Radian);
    public static readonly UnitOfMeasure Revolution = UnitFactory.Create("rev", 2 * Math.PI, Radian);
    public static readonly UnitOfMeasure Degree = UnitFactory.Create("°", Math.PI / 180, Radian);
    public static readonly UnitOfMeasure ArcMinute = UnitFactory.Create("′", 1d / 60, Degree);
    public static readonly UnitOfMeasure ArcSecond = UnitFactory.Create("″", 1d / 60, ArcMinute);
    public static readonly UnitOfMeasure Gradian = UnitFactory.Create("grad", 1d / 400, Revolution);
}
