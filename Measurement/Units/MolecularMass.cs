using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

public class MolecularMass : ReflectiveUnitList<MolecularMass>
{
    private MolecularMass() { }
    public static readonly MolecularMass Units = new();

    public static readonly Uom GramPerMole = UnitFactory.Create(
        (Mass.Gram, 1),
        (Moles.Mole, -1));

    public static readonly Uom KgPerKmol = UnitFactory.Create(
        (Mass.Kilogram, 1),
        (Moles.Kilomole, -1));

    public static readonly Uom GramPerKmol = UnitFactory.Create(
        (Mass.Gram, 1),
        (Moles.Kilomole, -1));

    public static readonly Uom KgPerMole = UnitFactory.Create(
        (Mass.Kilogram, 1),
        (Moles.Mole, -1));
}
