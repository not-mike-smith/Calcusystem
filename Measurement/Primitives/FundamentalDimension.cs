using System.Collections.Generic;
using System.Linq;
using System;
using Calcusystem.Measurement.Constants;
using Calcusystem.Measurement.Units;

namespace Calcusystem.Measurement.Primitives;

/// <summary>
/// One of the irreducible base dimensions of measurement (mass, length, time, …). A
/// <see cref="Dimensionality"/> is a map from these to integer exponents. Instances form a fixed, closed set:
/// use the static fields below rather than constructing new ones.
/// </summary>
/// <remarks>
/// Two instances are equal when their <see cref="Name"/>s match. A defined ordering (see <see cref="CompareTo"/>)
/// gives quantities a canonical symbol layout. Symbols are distinct even ignoring case, so that the
/// symbol-keyed serialization encoding cannot be corrupted by a stray case conversion.
/// </remarks>
public class FundamentalDimension : IComparable<FundamentalDimension>
{
    /// <summary>Human-readable name (e.g. "Electric Current"); also the identity used for equality.</summary>
    public string Name { get; }

    /// <summary>Short symbol used when formatting a dimensionality (e.g. "M", "L", "t").</summary>
    public string Symbol { get; }
    internal double QuantumValue { get; }
    internal double PlausibleMaximum { get; }

    private FundamentalDimension(
        string name,
        string symbol,
        double quantumValue,
        double maxValue)
    {
        Name = name;
        Symbol = symbol;
        QuantumValue = quantumValue;
        PlausibleMaximum = maxValue;
    }

    /// <summary>Mass (symbol M).</summary>
    public static readonly FundamentalDimension Mass = new (
        "Mass",
        "M",
        Limits.NeutrinoMass_kg,
        Limits.Universe.Mass_kg);

    /// <summary>Length (symbol L).</summary>
    public static readonly FundamentalDimension Length = new (
        "Length",
        "L",
        Limits.Planck.Length_m,
        Limits.Universe.Diameter_m);

    /// <summary>Thermodynamic temperature (symbol Θ, capital theta).</summary>
    public static readonly FundamentalDimension Temperature = new (
        "Temperature",
        "Θ",
        1e-9, // TODO: put a real guess
        Limits.Planck.Temperature_K);

    /// <summary>Electric current (symbol I).</summary>
    public static readonly FundamentalDimension ElectricCurrent = new (
        "Electric Current",
        "I",
        1 / ElectricCharge.ElectronsInCoulomb, // TODO put a real guess
        ElectricCharge.ElectronsInCoulomb); // TODO put a real guess

    /// <summary>Plane angle (symbol A) — a base dimension here so torque stays distinct from energy.</summary>
    public static readonly FundamentalDimension Angle = new (
        "Angle",
        "A",
        Limits.Planck.Length_m / Limits.Universe.Diameter_m,
        Limits.Universe.Diameter_m / Limits.Planck.Length_m); // TODO put a real guess

    /// <summary>Time (symbol T).</summary>
    public static readonly FundamentalDimension Time = new (
        "Time",
        "T",
        Limits.Planck.Time_s,
        Limits.Universe.Age_s);

    /// <summary>Amount of substance (symbol N).</summary>
    public static readonly FundamentalDimension AmountOfMatter = new (
        "Amount of Matter",
        "N",
        0.5 / Limits.AvogadrosNumber,
        Limits.Universe.MovingParts);

    /// <summary>Luminous intensity (symbol J).</summary>
    public static readonly FundamentalDimension LuminousIntensity = new (
        "Luminous Intensity",
        "J",
        1e-100, // TODO put a real guess
        1e100); // TODO put a real guess

    /// <summary>Monetary value (symbol C) — non-physical, supported for engineering-economics use.</summary>
    public static readonly FundamentalDimension Currency = new (
        "Currency",
        "C",
        1 / Limits.ZimbabweDenomination,
        Limits.Universe.Wealth_USD * Limits.ZimbabweDenomination);

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
