using Measurement.Models;

namespace Measurement.Factories;

public class Metric
{
    public readonly string Prefix;
    public readonly double Factor;

    public Metric(string prefix, double factor)
    {
        Prefix = prefix;
        Factor = factor;
    }

    public UnitOfMeasure Create(UnitOfMeasure unitOfMeasure)
    {
        return UnitFactory.Create(this, unitOfMeasure);
    }

    public static readonly Metric Kilo = new("k", 1E3);
    public static readonly Metric MInRomanNumerals = new Metric("M", 1E3);
    public static readonly Metric Mega = new("M", 1E6);
    public static readonly Metric MegaMega = new("MM", 1E6);
    public static readonly Metric Giga = new("G", 1E9);
    public static readonly Metric Tera = new("T", 1E12);
    public static readonly Metric Peta = new("P", 1E15);
    public static readonly Metric Exa = new("E", 1E18);
    public static readonly Metric Zetta = new("Z", 1E21);
    public static readonly Metric Yotta = new("Y", 1E24);

    public static readonly Metric Deci = new("d", 1E-1);
    public static readonly Metric Centi = new("c", 1E-2);
    public static readonly Metric Milli = new("m", 1E-3);
    public static readonly Metric Micro = new("μ", 1E-6);
    public static readonly Metric Nano = new("n", 1E-9);
    public static readonly Metric Pico = new("p", 1E-12);
    public static readonly Metric Femto = new("f", 1E-15);
    public static readonly Metric Atto = new("a", 1E-18);
    public static readonly Metric Zepto = new("z", 1E-21);
    public static readonly Metric Yocto = new("y", 1E-24);

    public static UnitOfMeasure k(UnitOfMeasure unitOfMeasure)
    {
        return Kilo.Create(unitOfMeasure);
    }

    public static UnitOfMeasure ThousandM(UnitOfMeasure unitOfMeasure)
    {
        return MInRomanNumerals.Create(unitOfMeasure);
    }

    public static UnitOfMeasure M(UnitOfMeasure unitOfMeasure)
    {
        return Mega.Create(unitOfMeasure);
    }

    public static UnitOfMeasure MM(UnitOfMeasure unitOfMeasure)
    {
        return MegaMega.Create(unitOfMeasure);
    }

    public static UnitOfMeasure G(UnitOfMeasure unitOfMeasure)
    {
        return Giga.Create(unitOfMeasure);
    }

    public static UnitOfMeasure T(UnitOfMeasure unitOfMeasure)
    {
        return Tera.Create(unitOfMeasure);
    }

    public static UnitOfMeasure P(UnitOfMeasure unitOfMeasure)
    {
        return Peta.Create(unitOfMeasure);
    }

    public static UnitOfMeasure E(UnitOfMeasure unitOfMeasure)
    {
        return Exa.Create(unitOfMeasure);
    }

    public static UnitOfMeasure Z(UnitOfMeasure unitOfMeasure)
    {
        return Zetta.Create(unitOfMeasure);
    }

    public static UnitOfMeasure Y(UnitOfMeasure unitOfMeasure)
    {
        return Yotta.Create(unitOfMeasure);
    }

    public static UnitOfMeasure d(UnitOfMeasure unitOfMeasure)
    {
        return Deci.Create(unitOfMeasure);
    }

    public static UnitOfMeasure c(UnitOfMeasure unitOfMeasure)
    {
        return Centi.Create(unitOfMeasure);
    }

    public static UnitOfMeasure m(UnitOfMeasure unitOfMeasure)
    {
        return Milli.Create(unitOfMeasure);
    }

    public static UnitOfMeasure micro(UnitOfMeasure unitOfMeasure)
    {
        return Micro.Create(unitOfMeasure);
    }

    public static UnitOfMeasure n(UnitOfMeasure unitOfMeasure)
    {
        return Nano.Create(unitOfMeasure);
    }

    public static UnitOfMeasure p(UnitOfMeasure unitOfMeasure)
    {
        return Pico.Create(unitOfMeasure);
    }

    public static UnitOfMeasure f(UnitOfMeasure unitOfMeasure)
    {
        return Femto.Create(unitOfMeasure);
    }

    public static UnitOfMeasure a(UnitOfMeasure unitOfMeasure)
    {
        return Atto.Create(unitOfMeasure);
    }

    public static UnitOfMeasure z(UnitOfMeasure unitOfMeasure)
    {
        return Zepto.Create(unitOfMeasure);
    }

    public static UnitOfMeasure y(UnitOfMeasure unitOfMeasure)
    {
        return Yocto.Create(unitOfMeasure);
    }
}
