using Calcusystem.Measurement.Factories;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Measurement.Units;

public class LuminousIntensity : ReflectiveUnitList<LuminousIntensity>
{
    private LuminousIntensity() { }
    public static readonly LuminousIntensity Units = new();

    public static readonly UnitOfMeasure Candela = UnitFactory.Create("cd", Dimensionality.LuminousIntensity, 1);
}
