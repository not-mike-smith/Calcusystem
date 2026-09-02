using Calcusystem.DimensionedExpression.BinaryOperators;


namespace Calcusystem.DimensionedExpression.Enums;

/// <summary>
/// How strongly two uncertain values support the claim that one is ordered against the other.
/// </summary>
/// <remarks>
/// A clean chain: each tier implies the one before it. Ordering is the family where that is true without
/// qualification — see <see cref="ContainmentLadder"/> for the one where it is not.
/// </remarks>
public enum OrderingConfidence : byte
{
    /// <summary>
    /// No pair of points drawn from the two intervals satisfies the ordering.
    /// </summary>
    /// <remarks>
    /// <b>A result, not a rung.</b> It is what you are left with when the weakest rung fails, so no rule tests
    /// it and <see cref="OrderingLadder.RuleFor"/> rejects it.
    /// </remarks>
    Contradicted = 1,

    /// <summary>Some pair does. The weakest claim worth making, and the one no named operator asked for.</summary>
    Possible = 2,

    /// <summary>The two reported values do, with their uncertainties set aside.</summary>
    Nominal = 3,

    /// <summary>Every pair does — the intervals do not overlap at all.</summary>
    Certain = 4,
}
