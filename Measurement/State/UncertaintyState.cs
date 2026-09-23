using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.Measurement.State;

/// <summary>
/// The complete stored state of an <see cref="IUncertainty"/> — enough to rebuild it, and nothing more.
/// Read it via <see cref="IUncertainty.GetState"/>; rebuild via <see cref="UncertaintyFactory.FromState"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the one place the storage convention (<see cref="IsStoredAsAbs"/> — whether the magnitudes are relative
/// fractions or absolute KMS errors) crosses the assembly boundary. It is deliberately a single narrow door rather
/// than a set of properties on the uncertainty classes themselves, whose public surface should only offer the
/// intended construction vocabulary.
/// </para>
/// <para>
/// Not a DTO: no type discriminator, no schema version. If the storage model gains a third form, this record
/// changes and every consumer gets a compile error at its mapping site — which is exactly where the decision about
/// migrating old data belongs.
/// </para>
/// </remarks>
public readonly record struct UncertaintyState
{
    /// <summary>Which concrete uncertainty this state rebuilds into.</summary>
    public UncertaintyShape Shape { get; private init; }

    /// <summary>Whether the magnitudes are absolute KMS errors (<c>true</c>) or relative fractions (<c>false</c>).</summary>
    public bool IsStoredAsAbs { get; private init; }

    /// <summary>The stored error above the nominal value. For <see cref="UncertaintyShape.Symmetric"/> this is
    /// the single magnitude, equal to <see cref="LowerMagnitude"/>.</summary>
    public double UpperMagnitude { get; private init; }

    /// <summary>The stored error below the nominal value.</summary>
    public double LowerMagnitude { get; private init; }

    /// <summary>Captures the state of a symmetric uncertainty.</summary>
    public static UncertaintyState Symmetric(bool isStoredAsAbs, double magnitude) => new()
    {
        Shape = UncertaintyShape.Symmetric,
        IsStoredAsAbs = isStoredAsAbs,
        UpperMagnitude = magnitude,
        LowerMagnitude = magnitude,
    };

    /// <summary>Captures the state of an asymmetric uncertainty.</summary>
    public static UncertaintyState Asymmetric(bool isStoredAsAbs, double upperMagnitude, double lowerMagnitude) => new()
    {
        Shape = UncertaintyShape.Asymmetric,
        IsStoredAsAbs = isStoredAsAbs,
        UpperMagnitude = upperMagnitude,
        LowerMagnitude = lowerMagnitude,
    };
}
