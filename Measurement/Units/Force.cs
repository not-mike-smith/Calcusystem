using Measurement.BaseClasses;
using Measurement.Factories;
using Measurement.Models;

namespace Measurement.Units;

public class Force : ReflectiveUnitList<Force>
{
    public static readonly Force Units = new();
    private Force() { }

    public static readonly UnitOfMeasure Newton = UnitFactory.Create(
        "N",
        (Mass.Kilogram, 1),
        (Acceleration.MeterPerSecondSquared, 1));

    public static readonly UnitOfMeasure CentiNewton = Metric.c(Newton);
    public static readonly UnitOfMeasure MilliNewton = Metric.m(Newton);
    public static readonly UnitOfMeasure MicroNewton = Metric.micro(Newton);
    public static readonly UnitOfMeasure NanoNewton = Metric.n(Newton);
    public static readonly UnitOfMeasure PicoNewton = Metric.p(Newton);
    public static readonly UnitOfMeasure FemtoNewton = Metric.f(Newton);
    public static readonly UnitOfMeasure AttoNewton = Metric.a(Newton);
    public static readonly UnitOfMeasure ZeptoNewton = Metric.z(Newton);
    public static readonly UnitOfMeasure YoctoNewton = Metric.y(Newton);

    public static readonly UnitOfMeasure KiloNewton = Metric.k(Newton);
    public static readonly UnitOfMeasure MegaNewton = Metric.M(Newton);
    public static readonly UnitOfMeasure GigaNewton = Metric.G(Newton);
    public static readonly UnitOfMeasure TeraNewton = Metric.T(Newton);
    public static readonly UnitOfMeasure PetaNewton = Metric.P(Newton);
    public static readonly UnitOfMeasure ExaNewton = Metric.E(Newton);
    public static readonly UnitOfMeasure ZettaNewton = Metric.Z(Newton);
    public static readonly UnitOfMeasure YottaNewton = Metric.Y(Newton);

    public static readonly UnitOfMeasure PoundForce = UnitFactory.Create(
        "lbf",
        32.174049,
        (Mass.Pound, 1),
        (Acceleration.FootPerSecondSquared, 1));
}
