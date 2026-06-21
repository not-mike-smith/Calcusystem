using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class ElectricPotential : ReflectiveUnitList<ElectricPotential>
{
    private ElectricPotential() { }
    public static readonly ElectricPotential Units = new();

    public static readonly Uom Volt = UnitFactory.Create("V", (Energy.Joule, 1), (ElectricCharge.Coulomb, -1));

    public static readonly Uom Picovolt = Metric.p(Volt);
    public static readonly Uom Nanovolt = Metric.n(Volt);
    public static readonly Uom Microvolt = Metric.micro(Volt);
    public static readonly Uom Millivolt = Metric.m(Volt);

    public static readonly Uom Kilovolt = Metric.k(Volt);
    public static readonly Uom Megavolt = Metric.M(Volt);
    public static readonly Uom Gigavolt = Metric.G(Volt);
}
