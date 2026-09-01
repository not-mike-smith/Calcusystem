using Calcusystem.Measurement.BaseClasses;
using Calcusystem.Measurement.Factories;
using Uom = Calcusystem.Measurement.Units.UnitOfMeasure;

namespace Calcusystem.Measurement.Units;

// Surface tension (M·T⁻²) — equivalently N/m or J/m².
// Additional metric prefix variants can be added using the Metric.k/M/m/micro etc. pattern.
public class SurfaceTension : ReflectiveUnitList<SurfaceTension>
{
    private SurfaceTension() { }
    public static readonly SurfaceTension Units = new();

    // SI
    public static readonly Uom NewtonPerMeter = UnitFactory.Create(
        "N/m",
        (Force.Newton, 1),
        (Length.Meter, -1));

    public static readonly Uom MillinewtonPerMeter = Metric.m(NewtonPerMeter);

    // CGS: 1 dyn/cm = 1 mN/m exactly
    public static readonly Uom DynePerCentimeter = UnitFactory.Create("dyn/cm", 0.001, NewtonPerMeter);
    public static readonly Uom MillidynePerCentimeter = Metric.m(DynePerCentimeter);
}
