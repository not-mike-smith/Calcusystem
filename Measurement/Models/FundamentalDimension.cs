using System;
using System.Collections.Generic;
using System.Linq;

namespace Measurement.Models;

/// <summary>
/// One of the irreducible base dimensions of measurement (mass, length, time, …). A
/// <see cref="Dimensionality"/> is a map from these to integer exponents. Instances form a fixed, closed set:
/// use the static fields below rather than constructing new ones.
/// </summary>
/// <remarks>
/// Two instances are equal when their <see cref="Name"/>s match. A defined ordering (see <see cref="CompareTo"/>)
/// gives quantities a canonical symbol layout. Note the case-sensitive symbols: <c>T</c> is temperature vs.
/// <c>t</c> time, and <c>C</c> is luminous intensity vs. <c>c</c> currency.
/// </remarks>
public class FundamentalDimension : IComparable<FundamentalDimension>
{
    /// <summary>Human-readable name (e.g. "Electric Current"); also the identity used for equality.</summary>
    public string Name { get; }

    /// <summary>Short symbol used when formatting a dimensionality (e.g. "M", "L", "t").</summary>
    public string Symbol { get; }

    private FundamentalDimension(string name, string symbol)
    {
        Name = name;
        Symbol = symbol;
    }

    /// <summary>Mass (symbol M).</summary>
    public static readonly FundamentalDimension Mass = new ("Mass", "M");

    /// <summary>Length (symbol L).</summary>
    public static readonly FundamentalDimension Length = new ("Length", "L");

    /// <summary>Thermodynamic temperature (symbol T, uppercase).</summary>
    public static readonly FundamentalDimension Temperature = new ("Temperature", "T");

    /// <summary>Electric current (symbol I).</summary>
    public static readonly FundamentalDimension ElectricCurrent = new ("Electric Current", "I");

    /// <summary>Plane angle (symbol A) — a base dimension here so torque stays distinct from energy.</summary>
    public static readonly FundamentalDimension Angle = new ("Angle", "A");

    /// <summary>Time (symbol t, lowercase).</summary>
    public static readonly FundamentalDimension Time = new ("Time", "t");

    /// <summary>Amount of substance (symbol N).</summary>
    public static readonly FundamentalDimension AmountOfMatter = new ("Amount of Matter", "N");

    /// <summary>Luminous intensity (symbol C, uppercase).</summary>
    public static readonly FundamentalDimension LuminousIntensity = new ("Luminous Intensity", "C");

    /// <summary>Monetary value (symbol c, lowercase) — non-physical, supported for engineering-economics use.</summary>
    public static readonly FundamentalDimension Currency = new ("Currency", "c");

    /// <summary>Canonical sort position of each dimension, used to lay out symbols consistently.</summary>
    internal static readonly IReadOnlyDictionary<FundamentalDimension, int> Order = new Dictionary<FundamentalDimension, int>
    {
        { Currency, 0 },
        { AmountOfMatter, 1 },
        { Mass, 2 },
        { LuminousIntensity, 3 },
        { ElectricCurrent, 4 },
        { Length, 5 },
        { Temperature, 6 },
        { Angle, 7 },
        { Time, 8 },
    };

    /// <summary>All fundamental dimensions in canonical order.</summary>
    public static readonly IReadOnlyList<FundamentalDimension> All = Order.Keys.ToList();

    /// <summary>Hash based on <see cref="Name"/>, consistent with <see cref="Equals"/>.</summary>
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    /// <summary>Equality by <see cref="Name"/>.</summary>
    public override bool Equals(object? obj)
    {
        return (obj as FundamentalDimension)?.Name == Name;
    }

    /// <summary>Returns the <see cref="Symbol"/>.</summary>
    public override string ToString()
    {
        return Symbol;
    }

    /// <summary>
    /// Orders by canonical position (see <see cref="All"/>). A <see langword="null"/> other sorts after this.
    /// </summary>
    public int CompareTo(FundamentalDimension? other)
    {
        if (other == null) return -1;
        return Order[this] - Order[other];
    }
}
