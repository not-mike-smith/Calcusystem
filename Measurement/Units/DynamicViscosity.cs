using Measurement.BaseClasses;
using Measurement.Factories;
using Uom = Measurement.Models.UnitOfMeasure;

namespace Measurement.Units;

// Dynamic viscosity (M·L⁻¹·t⁻¹ = Pa·s = kg/(m·s)).
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class DynamicViscosity : ReflectiveUnitList<DynamicViscosity>
{
    private DynamicViscosity() { }
    public static readonly DynamicViscosity Units = new();

    // SI
    public static readonly Uom PascalSecond = UnitFactory.Create(
        "Pa·s",
        (Pressure.Pascal, 1),
        (Time.Second, 1));

    public static readonly Uom MillipascalSecond = Metric.m(PascalSecond);

    // CGS
    public static readonly Uom Poise = UnitFactory.Create("P", 0.1, PascalSecond);
    public static readonly Uom Centipoise = Metric.c(Poise); // = 1 mPa·s

    // English Engineering
    public static readonly Uom PoundForceSecondPerSquareFoot = UnitFactory.Create(
        "lbf·s/ft²",
        (Force.PoundForce, 1),
        (Time.Second, 1),
        (Area.SquareFoot, -1));
}
