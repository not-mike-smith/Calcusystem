using Calcusystem.Measurement;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// How strongly two uncertain values support the claim that one is below the other.
/// </summary>
/// <remarks>
/// A clean chain: each tier implies the one before it. Ordering is the family where that is true without
/// qualification — see <see cref="ContainmentLadder"/> for the one where it is not.
/// </remarks>
public enum OrderingConfidence : byte
{
    /// <summary>No pair of points drawn from the two intervals satisfies the ordering.</summary>
    Contradicted = 1,

    /// <summary>Some pair does. The weakest claim worth making, and the one no named operator asked for.</summary>
    Possible = 2,

    /// <summary>The two reported values do, with their uncertainties set aside.</summary>
    Nominal = 3,

    /// <summary>Every pair does — the intervals do not overlap at all.</summary>
    Certain = 4,
}

/// <summary>
/// One evaluation of "is <c>lhs</c> below <c>rhs</c>", answering every tier at once.
/// </summary>
/// <remarks>
/// <para>
/// Under uncertainty a comparison does not have one answer, it has several nested ones. The named operators each
/// ask for a single tier and discard the rest, when the arithmetic that produces one produces all of them — so
/// the ladder computes the lot and each operator becomes a fixed-tier read.
/// </para>
/// <para>
/// Writing <c>a &gt; b</c> is asking this ladder about <c>(b, a)</c>. That is why four operators need one
/// evaluator: the greater-than family is the less-than family with the operands swapped.
/// </para>
/// <para>
/// Strictly ordered, and the implications are worth stating because the containment ladder's are not:
/// <see cref="Certain"/> ⟹ <see cref="Nominal"/> ⟹ <see cref="Possible"/>. Each follows from a nominal value
/// lying inside its own interval.
/// </para>
/// </remarks>
/// <param name="Possible">
/// Some point of <c>lhs</c> lies below some point of <c>rhs</c> — the intervals are not wholly the wrong way
/// round. <c>lhs.Lower &lt; rhs.Upper</c>.
/// </param>
/// <param name="Nominal">The reported values are ordered, uncertainty ignored. <c>lhs.Value &lt; rhs.Value</c>.</param>
/// <param name="Certain">
/// Every point of <c>lhs</c> lies below every point of <c>rhs</c>, so no uncertainty in either could reverse it.
/// <c>lhs.Upper &lt; rhs.Lower</c>.
/// </param>
public readonly record struct OrderingLadder(bool Possible, bool Nominal, bool Certain)
{
    /// <summary>The strongest tier this comparison reaches.</summary>
    public OrderingConfidence Achieved =>
        Certain ? OrderingConfidence.Certain
        : Nominal ? OrderingConfidence.Nominal
        : Possible ? OrderingConfidence.Possible
        : OrderingConfidence.Contradicted;

    /// <summary>Whether this comparison reaches at least <paramref name="tier"/>.</summary>
    public bool Reaches(OrderingConfidence tier) => Achieved >= tier;

    /// <summary>Evaluates every tier of "<paramref name="lhs"/> is below <paramref name="rhs"/>".</summary>
    /// <remarks>
    /// All three comparisons are strict, matching the named operators. <c>OPERATORS.md</c> records why there are
    /// no <c>≤</c> / <c>≥</c> variants: exact floating-point coincidence is unreachable in practice, so a
    /// non-strict ordering would differ from a strict one only on inputs that do not arise.
    /// </remarks>
    public static OrderingLadder Evaluate(Measurand lhs, Measurand rhs) =>
        new(Possible: lhs.KmsValue - lhs.KmsLowerAbsoluteError < rhs.KmsValue + rhs.KmsUpperAbsoluteError,
            Nominal: lhs.KmsValue < rhs.KmsValue,
            Certain: lhs.KmsValue + lhs.KmsUpperAbsoluteError < rhs.KmsValue - rhs.KmsLowerAbsoluteError);
}
