using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;

namespace Measurement.Units;

public class ElectricCurrent : ReflectiveUnitList<ElectricCurrent>
{
    private ElectricCurrent() { }
    public static readonly ElectricCurrent Units = new();

    public static readonly UnitOfMeasure Ampere = UnitFactory.Create("A", Dimensionality.ElectricCurrent, 1);
    public static readonly UnitOfMeasure Milliamp = Metric.m(Ampere);
    public static readonly UnitOfMeasure Microamp = Metric.micro(Ampere);
    public static readonly UnitOfMeasure Nanoamp = Metric.n(Ampere);
    public static readonly UnitOfMeasure Kiloamp = Metric.k(Ampere);
    public static readonly UnitOfMeasure ElectronsPerSec = UnitFactory.Create(
        "e⁻/s",
        1/ElectricCharge.ElectronsInCoulomb,
        Ampere);
}
