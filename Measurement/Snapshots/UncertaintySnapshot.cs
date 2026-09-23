using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Factories;

namespace Calcusystem.Measurement.Snapshots;

/// <summary>
/// The complete snapshot of an <see cref="IUncertainty"/> — enough to rebuild it, and nothing more.
/// Read it via <see cref="IUncertainty.GetSnapshot"/>; rebuild via <see cref="UncertaintyFactory.FromSnapshot"/>.
/// </summary>
/// <remarks>
/// This is the one place the storage convention (<see cref="IsStoredAsAbs"/> — whether the magnitudes are relative
/// fractions or absolute KMS magnitudes) crosses the assembly boundary. It is deliberately a single narrow door rather
/// than a set of properties on the uncertainty classes themselves, whose public surface should only offer the
/// intended construction vocabulary.
/// </remarks>
public readonly record struct UncertaintySnapshot
{
    /// <summary>Which concrete uncertainty this snapshot rebuilds into.</summary>
    public UncertaintyType Type { get; private init; }

    /// <summary>Whether the magnitudes are absolute KMS values (<c>true</c>) or relative fractions (<c>false</c>).</summary>
    public bool IsStoredAsAbs { get; private init; }

    /// <summary>The stored uncertainty above the nominal value. For <see cref="UncertaintyType.Symmetric"/> this is
    /// the single magnitude, equal to <see cref="LowerMagnitude"/>.</summary>
    public double UpperMagnitude { get; private init; }

    /// <summary>The stored uncertainty below the nominal value.</summary>
    public double LowerMagnitude { get; private init; }

    /// <summary>Captures a symmetric uncertainty.</summary>
    public static UncertaintySnapshot Symmetric(bool isStoredAsAbs, double magnitude) => new()
    {
        Type = UncertaintyType.Symmetric,
        IsStoredAsAbs = isStoredAsAbs,
        UpperMagnitude = magnitude,
        LowerMagnitude = magnitude,
    };

    /// <summary>Captures an asymmetric uncertainty.</summary>
    public static UncertaintySnapshot Asymmetric(bool isStoredAsAbs, double upperMagnitude, double lowerMagnitude) => new()
    {
        Type = UncertaintyType.Asymmetric,
        IsStoredAsAbs = isStoredAsAbs,
        UpperMagnitude = upperMagnitude,
        LowerMagnitude = lowerMagnitude,
    };
}
