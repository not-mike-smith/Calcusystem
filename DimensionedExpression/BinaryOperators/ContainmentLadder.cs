using Calcusystem.Measurement;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// One evaluation of "is <c>lhs</c> inside <c>rhs</c>'s tolerance band", answering every rung at once.
/// </summary>
/// <remarks>
/// <para>
/// The same idea as <see cref="OrderingLadder"/> — compute every nested answer from the one pass that produces
/// any of them — but the middle rungs form a <b>lattice, not a chain</b>. A value's upper and lower bounds are
/// independently checkable, so <see cref="NominalAndUpperWithin"/> and <see cref="NominalAndLowerWithin"/> are
/// incomparable: either can hold without the other. That is why there is no single ordered <c>Achieved</c> here
/// as there is for ordering; asking for one would force a total order onto rungs that genuinely lack it.
/// </para>
/// <para>
/// Each rung is declared below as the <see cref="ComparisonRule"/>s it consists of, and the containment
/// operators point at those declarations rather than restating them — so a rung and the operator named after it
/// cannot drift apart.
/// </para>
/// <para>
/// The implications that <i>do</i> hold all run downward, which is what makes the ladder sound:
/// <see cref="WhollyWithin"/> ⟹ both middle rungs ⟹ <see cref="NominalWithin"/> ⟹ <see cref="Overlaps"/>.
/// </para>
/// <para>
/// <b>The converse deliberately fails at the top.</b> Both middle rungs together do not reach
/// <see cref="WhollyWithin"/>, because that rung is <i>strict</i> on both bounds while every other rung is not.
/// Two identical intervals therefore satisfy every rung but the last. That is long-standing intended behaviour
/// — an interval is not <i>strictly</i> inside a copy of itself — and it is the one boundary coincidence that
/// really does arise, since comparing a value against a spec built from the same figures is ordinary.
/// </para>
/// </remarks>
/// <param name="Overlaps">
/// The two intervals share at least one point, so the values are not incompatible. Symmetric — at this rung the
/// asymmetry between subject and band genuinely vanishes. Non-strict: intervals that merely touch overlap.
/// </param>
/// <param name="NominalWithin">
/// <c>lhs</c>'s reported value falls inside <c>rhs</c>'s band, with <c>lhs</c>'s own uncertainty set aside.
/// </param>
/// <param name="NominalAndUpperWithin">
/// …and <c>lhs</c> cannot overshoot the band's ceiling even at its worst case. The rung for a maximum rating.
/// </param>
/// <param name="NominalAndLowerWithin">
/// …and <c>lhs</c> cannot undershoot the band's floor even at its worst case. The rung for a minimum rating.
/// </param>
/// <param name="WhollyWithin">
/// The whole of <c>lhs</c>'s interval lies strictly inside <c>rhs</c>'s, so no part of its uncertainty reaches
/// the band's edge.
/// </param>
public readonly record struct ContainmentLadder(
    bool? Overlaps,
    bool? NominalWithin,
    bool? NominalAndUpperWithin,
    bool? NominalAndLowerWithin,
    bool? WhollyWithin)
{
    /// <summary>The subject's reported value is at or above the band's floor.</summary>
    public static readonly ComparisonRule AboveFloor =
        new(Landmark.Nominal, ComparisonType.GreaterThanOrEqualTo, Landmark.LowerBound);

    /// <summary>The subject's reported value is at or below the band's ceiling.</summary>
    public static readonly ComparisonRule BelowCeiling =
        new(Landmark.Nominal, ComparisonType.LessThanOrEqualTo, Landmark.UpperBound);

    /// <summary>The rungs of this ladder, as the rules each one asserts.</summary>
    /// <remarks>
    /// <see cref="OverlapsRules"/> is stated as "neither interval ends before the other begins" rather than as
    /// two containments, which is what makes its symmetry visible: mirroring either rule gives the other.
    /// </remarks>
    public static readonly IReadOnlyList<ComparisonRule> OverlapsRules =
    [
        new(Landmark.UpperBound, ComparisonType.GreaterThanOrEqualTo, Landmark.LowerBound),
        new(Landmark.LowerBound, ComparisonType.LessThanOrEqualTo, Landmark.UpperBound),
    ];

    /// <inheritdoc cref="OverlapsRules"/>
    public static readonly IReadOnlyList<ComparisonRule> NominalWithinRules = [AboveFloor, BelowCeiling];

    /// <inheritdoc cref="OverlapsRules"/>
    public static readonly IReadOnlyList<ComparisonRule> NominalAndUpperWithinRules =
    [
        AboveFloor,
        new(Landmark.UpperBound, ComparisonType.LessThanOrEqualTo, Landmark.UpperBound),
    ];

    /// <inheritdoc cref="OverlapsRules"/>
    public static readonly IReadOnlyList<ComparisonRule> NominalAndLowerWithinRules =
    [
        BelowCeiling,
        new(Landmark.LowerBound, ComparisonType.GreaterThanOrEqualTo, Landmark.LowerBound),
    ];

    /// <inheritdoc cref="OverlapsRules"/>
    public static readonly IReadOnlyList<ComparisonRule> WhollyWithinRules =
    [
        new(Landmark.LowerBound, ComparisonType.GreaterThan, Landmark.LowerBound),
        new(Landmark.UpperBound, ComparisonType.LessThan, Landmark.UpperBound),
    ];

    /// <summary>Evaluates every rung of "<paramref name="lhs"/> is within <paramref name="rhs"/>".</summary>
    /// <remarks>
    /// The middle rungs restate <see cref="AboveFloor"/> and <see cref="BelowCeiling"/> even though each is
    /// implied by the bound test beside it — a nominal value always lies between its own bounds. Relying on that
    /// implication would make a rung's condition depend on reasoning done elsewhere, and the implication is one
    /// tolerance-aware comparison away from being only nearly true.
    /// </remarks>
    public static ContainmentLadder Evaluate(Measurand lhs, Measurand rhs) =>
        new(Overlaps: ComparisonRule.AllSatisfied(OverlapsRules, lhs, rhs),
            NominalWithin: ComparisonRule.AllSatisfied(NominalWithinRules, lhs, rhs),
            NominalAndUpperWithin: ComparisonRule.AllSatisfied(NominalAndUpperWithinRules, lhs, rhs),
            NominalAndLowerWithin: ComparisonRule.AllSatisfied(NominalAndLowerWithinRules, lhs, rhs),
            WhollyWithin: ComparisonRule.AllSatisfied(WhollyWithinRules, lhs, rhs));
}
