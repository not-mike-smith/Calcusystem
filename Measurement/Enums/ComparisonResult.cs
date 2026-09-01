namespace Calcusystem.Measurement.Enums;

/// <summary>
/// How one value stands to another: below it, above it, the same as it, or not comparable to it at all.
/// </summary>
/// <remarks>
/// <para>
/// One bit each, and mutually exclusive — a comparison produces exactly one of these. The single-bit layout is
/// what lets <see cref="ComparisonType"/> be a mask of acceptable outcomes; see that enum for why the two are
/// kept together.
/// </para>
/// <para>
/// <see cref="Incomparable"/> is zero, so it is accepted by no mask. It means the question has no answer —
/// different dimensions, or a value that is not a number — and is never a substitute for a negative answer.
/// </para>
/// </remarks>
public enum ComparisonResult : byte
{
    /// <summary>The question has no answer. Distinct from any of the three orderings, and never a guess.</summary>
    Incomparable = 0b000,

    /// <summary>The left value exceeds the right.</summary>
    GreaterThan = 0b001,

    /// <summary>The left value falls below the right.</summary>
    LessThan = 0b010,

    /// <summary>The two agree — exactly, or within what the measurements can resolve.</summary>
    Equal = 0b100,
}
