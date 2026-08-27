using Calcusystem.Measurement;

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
    bool Overlaps,
    bool NominalWithin,
    bool NominalAndUpperWithin,
    bool NominalAndLowerWithin,
    bool WhollyWithin)
{
    /// <summary>Evaluates every rung of "<paramref name="lhs"/> is within <paramref name="rhs"/>".</summary>
    public static ContainmentLadder Evaluate(Measurand lhs, Measurand rhs)
    {
        var subjectFloor = lhs.KmsValue - lhs.KmsLowerAbsoluteError;
        var subjectCeiling = lhs.KmsValue + lhs.KmsUpperAbsoluteError;
        var bandFloor = rhs.KmsValue - rhs.KmsLowerAbsoluteError;
        var bandCeiling = rhs.KmsValue + rhs.KmsUpperAbsoluteError;

        var aboveFloor = lhs.KmsValue >= bandFloor;
        var belowCeiling = lhs.KmsValue <= bandCeiling;

        // `aboveFloor` is implied by a ceiling that fits, and `belowCeiling` by a floor that fits, since a
        // nominal value always lies between its own bounds. Both are still written out: relying on the
        // implication would make each rung's condition depend on reasoning done somewhere else.
        return new ContainmentLadder(
            Overlaps: subjectCeiling >= bandFloor && bandCeiling >= subjectFloor,
            NominalWithin: aboveFloor && belowCeiling,
            NominalAndUpperWithin: aboveFloor && subjectCeiling <= bandCeiling,
            NominalAndLowerWithin: belowCeiling && subjectFloor >= bandFloor,
            WhollyWithin: subjectFloor > bandFloor && subjectCeiling < bandCeiling);
    }
}
