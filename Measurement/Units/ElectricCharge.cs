using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;

namespace Measurement.Units;

public class ElectricCharge : ReflectiveUnitList<ElectricCharge>
{
    public const double ElectronsInCoulomb = 6.241509074E18;

    private ElectricCharge() { }
    public static readonly ElectricCharge Units = new();

    public static readonly UnitOfMeasure Coulomb = UnitFactory.Create(
        "C",
        Dimensionality.ElectricCurrent * Dimensionality.Time,
        1);

    public static readonly UnitOfMeasure FundamentalCharge = UnitFactory.Create("e⁻", 1 / ElectronsInCoulomb, Coulomb);
}
