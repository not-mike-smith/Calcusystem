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

    /// <summary>
    /// 10^3
    /// </summary>
    public static readonly Metric Kilo = new("k", 1E3);

    /// <summary>
    /// 10^3, but M is also used for Mega, so this is MInRomanNumerals
    /// </summary>
    public static readonly Metric MInRomanNumerals = new Metric("M", 1E3);

    /// <summary>
    /// 10^6
    /// </summary>
    public static readonly Metric Mega = new("M", 1E6);

    /// <summary>
    /// 10^6, but M is also used for Mega, so this is MMInRomanNumerals
    /// </summary>
    public static readonly Metric MegaMega = new("MM", 1E6);

    /// <summary>
    /// 10^9
    /// </summary>
    public static readonly Metric Giga = new("G", 1E9);

    /// <summary>
    /// 10^12
    /// </summary>
    public static readonly Metric Tera = new("T", 1E12);

    /// <summary>
    /// 10^15
    /// </summary>
    public static readonly Metric Peta = new("P", 1E15);

    /// <summary>
    /// 10^18
    /// </summary>
    public static readonly Metric Exa = new("E", 1E18);

    /// <summary>
    /// 10^21
    /// </summary>
    public static readonly Metric Zetta = new("Z", 1E21);

    /// <summary>
    /// 10^24
    /// </summary>
    public static readonly Metric Yotta = new("Y", 1E24);

    /// <summary>
    /// 10^-1
    /// </summary>
    public static readonly Metric Deci = new("d", 1E-1);

    /// <summary>
    /// 10^-2
    /// </summary>
    public static readonly Metric Centi = new("c", 1E-2);

    /// <summary>
    /// 10^-3
    /// </summary>
    public static readonly Metric Milli = new("m", 1E-3);

    /// <summary>
    /// 10^-6
    /// </summary>
    public static readonly Metric Micro = new("μ", 1E-6);

    /// <summary>
    /// 10^-9
    /// </summary>
    public static readonly Metric Nano = new("n", 1E-9);

    /// <summary>
    /// 10^-12
    /// </summary>
    public static readonly Metric Pico = new("p", 1E-12);

    /// <summary>
    /// 10^-15
    /// </summary>
    public static readonly Metric Femto = new("f", 1E-15);

    /// <summary>
    /// 10^-18
    /// </summary>
    public static readonly Metric Atto = new("a", 1E-18);

    /// <summary>
    /// 10^-21
    /// </summary>
    public static readonly Metric Zepto = new("z", 1E-21);

    /// <summary>
    /// 10^-24
    /// </summary>
    public static readonly Metric Yocto = new("y", 1E-24);

    /// <summary>
    /// kilo, 10^3
    /// </summary>
    public static UnitOfMeasure k(UnitOfMeasure unitOfMeasure)
    {
        return Kilo.Create(unitOfMeasure);
    }

    /// <summary>
    /// M, 10^3, but M is also used for Mega, so this is MInRomanNumerals
    /// </summary>
    public static UnitOfMeasure ThousandM(UnitOfMeasure unitOfMeasure)
    {
        return MInRomanNumerals.Create(unitOfMeasure);
    }

    /// <summary>
    /// Mega, 10^6
    /// </summary>
    public static UnitOfMeasure M(UnitOfMeasure unitOfMeasure)
    {
        return Mega.Create(unitOfMeasure);
    }

    /// <summary>
    /// MM, 10^6 (MegaMega)
    /// </summary>
    public static UnitOfMeasure MM(UnitOfMeasure unitOfMeasure)
    {
        return MegaMega.Create(unitOfMeasure);
    }

    /// <summary>
    /// Giga, 10^9
    /// </summary>
    public static UnitOfMeasure G(UnitOfMeasure unitOfMeasure)
    {
        return Giga.Create(unitOfMeasure);
    }

    /// <summary>
    /// Tera, 10^12
    /// </summary>
    public static UnitOfMeasure T(UnitOfMeasure unitOfMeasure)
    {
        return Tera.Create(unitOfMeasure);
    }

    /// <summary>
    /// Peta, 10^15
    /// </summary>
    public static UnitOfMeasure P(UnitOfMeasure unitOfMeasure)
    {
        return Peta.Create(unitOfMeasure);
    }

    /// <summary>
    /// Exa, 10^18
    /// </summary>
    public static UnitOfMeasure E(UnitOfMeasure unitOfMeasure)
    {
        return Exa.Create(unitOfMeasure);
    }

    /// <summary>
    /// Zetta, 10^21
    /// </summary>
    public static UnitOfMeasure Z(UnitOfMeasure unitOfMeasure)
    {
        return Zetta.Create(unitOfMeasure);
    }

    /// <summary>
    /// Yotta, 10^24
    /// </summary>
    public static UnitOfMeasure Y(UnitOfMeasure unitOfMeasure)
    {
        return Yotta.Create(unitOfMeasure);
    }

    /// <summary>
    /// Deci, 10^-1
    /// </summary>
    public static UnitOfMeasure d(UnitOfMeasure unitOfMeasure)
    {
        return Deci.Create(unitOfMeasure);
    }

    /// <summary>
    /// Centi, 10^-2
    /// </summary>
    public static UnitOfMeasure c(UnitOfMeasure unitOfMeasure)
    {
        return Centi.Create(unitOfMeasure);
    }

    /// <summary>
    /// Milli, 10^-3
    /// </summary>
    public static UnitOfMeasure m(UnitOfMeasure unitOfMeasure)
    {
        return Milli.Create(unitOfMeasure);
    }

    /// <summary>
    /// Micro, 10^-6 (μ)
    /// </summary>
    public static UnitOfMeasure micro(UnitOfMeasure unitOfMeasure)
    {
        return Micro.Create(unitOfMeasure);
    }

    /// <summary>
    /// Nano, 10^-9
    /// </summary>
    public static UnitOfMeasure n(UnitOfMeasure unitOfMeasure)
    {
        return Nano.Create(unitOfMeasure);
    }

    /// <summary>
    /// Pico, 10^-12
    /// </summary>
    public static UnitOfMeasure p(UnitOfMeasure unitOfMeasure)
    {
        return Pico.Create(unitOfMeasure);
    }

    /// <summary>
    /// Femto, 10^-15
    /// </summary>
    public static UnitOfMeasure f(UnitOfMeasure unitOfMeasure)
    {
        return Femto.Create(unitOfMeasure);
    }

    /// <summary>
    /// Atto, 10^-18
    /// </summary>
    public static UnitOfMeasure a(UnitOfMeasure unitOfMeasure)
    {
        return Atto.Create(unitOfMeasure);
    }

    /// <summary>
    /// Zepto, 10^-21
    /// </summary>
    public static UnitOfMeasure z(UnitOfMeasure unitOfMeasure)
    {
        return Zepto.Create(unitOfMeasure);
    }

    /// <summary>
    /// Yocto, 10^-24
    /// </summary>
    public static UnitOfMeasure y(UnitOfMeasure unitOfMeasure)
    {
        return Yocto.Create(unitOfMeasure);
    }
}
