using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// The vocabulary of containment: which rung a set of comparisons <i>is</i>, and how far into the band a subject
/// actually gets.
/// </summary>
/// <remarks>
/// <para>
/// A classifier, like <see cref="OrderingLadder"/> and for the same reasons. It used to be a record struct that
/// evaluated all five rungs eagerly — ten comparisons to answer whichever one was asked — and nothing outside
/// the tests ever called it. Operators no longer route through it either: three of the five rungs had exactly
/// one consumer, so pointing at a shared constant bought nothing and cost a reader the trip to find out what an
/// operator checks.
/// </para>
/// <para>
/// The implications that hold all run downward, which is what makes the ladder sound:
/// <see cref="ContainmentRung.WhollyWithin"/> ⟹ both middle rungs ⟹ <see cref="ContainmentRung.NominalWithin"/>
/// ⟹ <see cref="ContainmentRung.Overlaps"/>.
/// </para>
/// <para>
/// <b>The converse deliberately fails at the top.</b> Both middle rungs together do not reach
/// <see cref="ContainmentRung.WhollyWithin"/>, because that rung is <i>strict</i> on both bounds while every
/// other rung is not. Two identical intervals therefore satisfy every rung but the last — an interval is not
/// <i>strictly</i> inside a copy of itself. That is intended: the dot-prefixed operators place a <b>point</b> in
/// a <i>closed</i> band, while <c>[=}</c> places an <b>interval</b> inside an <i>open</i> one, and it is the one
/// boundary coincidence that really arises, since checking a value against a spec built from the same figures is
/// ordinary.
/// </para>
/// </remarks>
public static class ContainmentLadder
{
    private static readonly ComparisonRule AboveFloor =
        new(Landmark.Nominal, ComparisonType.GreaterThanOrEqualTo, Landmark.LowerBound);

    private static readonly ComparisonRule BelowCeiling =
        new(Landmark.Nominal, ComparisonType.LessThanOrEqualTo, Landmark.UpperBound);

    private static readonly ContainmentRung[] AllRungs = Enum.GetValues<ContainmentRung>();

    /// <summary>The comparisons that together test a given rung.</summary>
    /// <remarks>
    /// <para>
    /// <see cref="ContainmentRung.Overlaps"/> is stated as "neither interval ends before the other begins" rather
    /// than as two containments, which is what makes its symmetry visible: mirroring either rule gives the other.
    /// </para>
    /// <para>
    /// The middle rungs restate <see cref="AboveFloor"/> or <see cref="BelowCeiling"/> even though each is
    /// implied by the bound test beside it — a nominal value always lies between its own bounds. Relying on that
    /// implication would make a rung's condition depend on reasoning done elsewhere, and it is one
    /// tolerance-aware comparison away from being only nearly true.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<ComparisonRule> RulesFor(ContainmentRung rung) => rung switch
    {
        ContainmentRung.Overlaps =>
        [
            new(Landmark.UpperBound, ComparisonType.GreaterThanOrEqualTo, Landmark.LowerBound),
            new(Landmark.LowerBound, ComparisonType.LessThanOrEqualTo, Landmark.UpperBound),
        ],
        ContainmentRung.NominalWithin => [AboveFloor, BelowCeiling],
        ContainmentRung.NominalAndUpperWithin =>
        [
            AboveFloor,
            new(Landmark.UpperBound, ComparisonType.LessThanOrEqualTo, Landmark.UpperBound),
        ],
        ContainmentRung.NominalAndLowerWithin =>
        [
            BelowCeiling,
            new(Landmark.LowerBound, ComparisonType.GreaterThanOrEqualTo, Landmark.LowerBound),
        ],
        ContainmentRung.WhollyWithin =>
        [
            new(Landmark.LowerBound, ComparisonType.GreaterThan, Landmark.LowerBound),
            new(Landmark.UpperBound, ComparisonType.LessThan, Landmark.UpperBound),
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(rung), rung, "Unknown containment rung."),
    };

    /// <summary>
    /// Which rung <paramref name="rules"/> is, or <see langword="null"/> where they are not one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The discovery half, and what lets an operator declare its comparisons plainly and still be placed on the
    /// ladder. Order-insensitive, since a set of rules is a conjunction.
    /// </para>
    /// <para>
    /// Matches on the rules themselves, not on what they mean. Two differently written sets that happen to be
    /// equivalent will not be recognised — <c>MutuallyWithinTolerance</c> is nominal containment in both
    /// directions and is correctly <i>not</i> a rung, but a set that merely restated a rung with a redundant
    /// term would also come back null. Semantic matching is possible and considerably more machinery; nothing
    /// needs it yet.
    /// </para>
    /// </remarks>
    public static ContainmentRung? RungOf(IReadOnlyList<ComparisonRule> rules)
    {
        foreach (var rung in AllRungs)
        {
            var candidate = RulesFor(rung);
            if (candidate.Count == rules.Count && !candidate.Except(rules).Any()) return rung;
        }

        return null;
    }

    /// <summary>
    /// Whether <paramref name="lhs"/> sits inside <paramref name="rhs"/>'s band at least as far as
    /// <paramref name="rung"/>, or <see langword="null"/> where that cannot be settled.
    /// </summary>
    /// <remarks>
    /// Evaluates only the rung asked for. A single rung can be unknown while its neighbours answer — two
    /// unbounded uncertainties leave ceiling-against-ceiling undecidable while the reported values still say
    /// perfectly well where they sit — so this is per rung rather than a reading off one shared evaluation.
    /// </remarks>
    public static bool? Reaches(Measurand lhs, Measurand rhs, ContainmentRung rung) =>
        ComparisonRule.AllSatisfied(RulesFor(rung), lhs, rhs);
}
