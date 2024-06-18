using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class Moles : ReflectiveUnitList<Moles>
{
    private Moles() { }
    public static readonly Moles Units = new();

    public static readonly Uom Kilomole = UnitFactory.Create("kmol", Dimensionality.AmountOfMatter, 1);
    public static readonly Uom Mole = UnitFactory.Create("mol", 0.001, Kilomole);
    public static readonly Uom Millimole = Metric.m(Mole);
    public static readonly Uom Micromole = Metric.micro(Mole);
    public static readonly Uom Nanomole = Metric.n(Mole);
    public static readonly Uom Megamole = Metric.M(Mole);
}
