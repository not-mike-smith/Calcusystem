using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class ElectricResistance : ReflectiveUnitList<ElectricResistance>
{
    private ElectricResistance() { }
    public static readonly ElectricResistance Units = new();

    public static readonly Uom Ohm = UnitFactory.Create("Ω", (ElectricPotential.Volt, 1), (ElectricCurrent.Ampere, -1));

    public static readonly Uom Microohm = Metric.micro(Ohm);
    public static readonly Uom Milliohm = Metric.m(Ohm);

    public static readonly Uom Kiloohm = Metric.k(Ohm);
    public static readonly Uom Megaohm = Metric.M(Ohm);
    public static readonly Uom Gigaohm = Metric.G(Ohm);
}
