using Calcusystem.Measurement;
using Calcusystem.Measurement.Enums;

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
/// <b>The tiers are declared here, as <see cref="ComparisonRule"/>s, and the operators point at them.</b> That
/// is what keeps a tier and the operator named after it from being two descriptions that could drift: there is
/// one triple per tier, in one place, and <c>DefinitelyLessThanOperator</c> is literally
/// <see cref="Certainly"/>. The greater-than family reaches the same triples through
/// <see cref="ComparisonRule.Mirrored"/>, so a swapped reading needs no second declaration either.
/// </para>
/// <para>
/// Strictly ordered, and the implications are worth stating because the containment ladder's are not:
/// <see cref="Certain"/> ⟹ <see cref="Nominal"/> ⟹ <see cref="Possible"/>. Each follows from a nominal value
/// lying inside its own interval.
/// </para>
/// <para>
/// Every rung is three-valued, because a comparison that cannot be answered must not read as a denial. For
/// <i>this</i> ladder the rungs go unknown together in practice — its comparisons pit a ceiling against a floor,
/// and infinities of opposite sign are perfectly well ordered, so only a value that is not a number silences it,
/// and that silences all three at once. <see cref="ContainmentLadder"/> is where a single rung genuinely drops
/// out on its own: it compares ceiling against ceiling, which two unbounded uncertainties leave undecidable
/// while the reported values still answer.
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
public readonly record struct OrderingLadder(bool? Possible, bool? Nominal, bool? Certain)
{
    /// <summary>The <see cref="OrderingConfidence.Possible"/> tier: <c>lhs.Lower &lt; rhs.Upper</c>.</summary>
    public static readonly ComparisonRule Possibly =
        new(Landmark.LowerBound, ComparisonType.LessThan, Landmark.UpperBound);

    /// <summary>The <see cref="OrderingConfidence.Nominal"/> tier: <c>lhs.Value &lt; rhs.Value</c>.</summary>
    public static readonly ComparisonRule Nominally =
        new(Landmark.Nominal, ComparisonType.LessThan, Landmark.Nominal);

    /// <summary>The <see cref="OrderingConfidence.Certain"/> tier: <c>lhs.Upper &lt; rhs.Lower</c>.</summary>
    public static readonly ComparisonRule Certainly =
        new(Landmark.UpperBound, ComparisonType.LessThan, Landmark.LowerBound);

    /// <summary>The strongest tier this comparison reaches, or null where that cannot be settled.</summary>
    /// <remarks>
    /// Read from the top down, so an unanswered rung only obscures the tiers at or above it: a comparison that
    /// is certainly ordered is certainly ordered whatever the weaker rungs say, but one whose strongest rung is
    /// unknown might belong to any tier from there up.
    /// </remarks>
    public OrderingConfidence? Achieved => Certain switch
    {
        true => OrderingConfidence.Certain,
        null => null,
        false => Nominal switch
        {
            true => OrderingConfidence.Nominal,
            null => null,
            false => Possible switch
            {
                true => OrderingConfidence.Possible,
                null => null,
                false => OrderingConfidence.Contradicted,
            },
        },
    };

    /// <summary>Whether this comparison reaches at least <paramref name="tier"/>.</summary>
    /// <remarks>
    /// Each tier is exactly one rung, so this reads the rung rather than deriving it from
    /// <see cref="Achieved"/> — which would needlessly answer "unknown" for a tier that is settled just because
    /// a stronger one is not.
    /// </remarks>
    public bool? Reaches(OrderingConfidence tier) => tier switch
    {
        OrderingConfidence.Contradicted => true,
        OrderingConfidence.Possible => Possible,
        OrderingConfidence.Nominal => Nominal,
        OrderingConfidence.Certain => Certain,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown ordering tier."),
    };

    /// <summary>Evaluates every tier of "<paramref name="lhs"/> is below <paramref name="rhs"/>".</summary>
    /// <remarks>
    /// Every comparison runs through <see cref="MeasurandComparer"/>, so "below" here means below by more than
    /// the measurements can resolve. Two values that differ only by floating-point drift are not ordered by any
    /// tier, which is the point: an ordering conjured out of the last bits of a mantissa is not one an engineer
    /// asked for. <c>OPERATORS.md</c> records why there are no <c>≤</c> / <c>≥</c> variants of these tiers.
    /// </remarks>
    public static OrderingLadder Evaluate(Measurand lhs, Measurand rhs) =>
        new(Possible: Possibly.IsSatisfiedGiven(lhs, rhs),
            Nominal: Nominally.IsSatisfiedGiven(lhs, rhs),
            Certain: Certainly.IsSatisfiedGiven(lhs, rhs));
}
