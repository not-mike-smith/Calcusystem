using Calcusystem.Measurement.BaseClasses;
using Calcusystem.Measurement.Dimensions;
using Calcusystem.Measurement.Factories;

namespace Calcusystem.Measurement.Units;

public class LuminousIntensity : ReflectiveUnitList<LuminousIntensity>
{
    private LuminousIntensity() { }
    public static readonly LuminousIntensity Units = new();

    public static readonly UnitOfMeasure Candela = UnitFactory.Create("cd", Dimensionality.LuminousIntensity, 1);
}
