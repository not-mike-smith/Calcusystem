using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>One rung of the ordering ladder: a direction and a strength.</summary>
/// <param name="Direction">Which way the claim runs.</param>
/// <param name="Confidence">
/// How strong the claim is. <see cref="OrderingConfidence.Contradicted"/> is not a rung — see
/// <see cref="OrderingLadder.RuleFor"/>.
/// </param>
public readonly record struct OrderingRung(OrderingDirection Direction, OrderingConfidence Confidence)
{
    /// <summary>The comparison that tests this rung.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="Confidence"/> is <see cref="OrderingConfidence.Contradicted"/>, which no rule tests.
    /// </exception>
    public ComparisonRule Rule => OrderingLadder.RuleFor(Direction, Confidence);

    public override string ToString() => $"{Direction}/{Confidence} ({Rule.Symbol})";
}

/// <summary>
/// The vocabulary of ordering strength: which rung a comparison <i>is</i>, and how far up the ladder two values
/// actually get.
/// </summary>
/// <remarks>
/// <para>
/// Under uncertainty a comparison does not have one answer, it has several nested ones —
/// <see cref="OrderingConfidence.Certain"/> ⟹ <see cref="OrderingConfidence.Nominal"/> ⟹
/// <see cref="OrderingConfidence.Possible"/>, each following from a nominal value lying inside its own interval.
/// </para>
/// <para>
/// <b>A classifier, not an evaluator.</b> It used to be a record struct that computed all three tiers eagerly,
/// which was waste dressed up as insight: an operator asserts one rung and discards the rest, and nothing in the
/// library ever wanted the other two. Worse, it put the ladder between an operator and the comparison it makes,
/// so reading <c>DefinitelyLessThanOperator</c> meant knowing that tiers are less-than by convention unless
/// marked otherwise. Operators now declare their own rules plainly, and the ladder is asked afterwards.
/// </para>
/// <para>
/// What the ladder is genuinely for is <i>reporting</i>. A modeller who writes <c>·&lt;·</c> and gets
/// <see langword="false"/> cannot otherwise tell "comfortably the other way round" from "a hair's breadth away,
/// and the uncertainty covers it" — <see cref="AchievedTier"/> answers that, and only when asked.
/// </para>
/// </remarks>
public static class OrderingLadder
{
    /// <summary>The testable tiers, weakest first. Declared before <see cref="Rungs"/>, which reads it.</summary>
    private static readonly OrderingConfidence[] Strengthening =
        [OrderingConfidence.Possible, OrderingConfidence.Nominal, OrderingConfidence.Certain];

    /// <summary>Every rung, in both directions.</summary>
    /// <remarks>
    /// Derived from <see cref="RuleFor"/> rather than listed separately, so the classifier and the definitions
    /// are one table rather than two that could disagree.
    /// </remarks>
    private static readonly OrderingRung[] Rungs =
    [
        .. from direction in new[] { OrderingDirection.Below, OrderingDirection.Above }
           from confidence in Strengthening
           select new OrderingRung(direction, confidence),
    ];

    /// <summary>The comparison that tests a given rung.</summary>
    /// <remarks>
    /// <para>
    /// The <see cref="OrderingDirection.Below"/> rules are the definitions; <see cref="OrderingDirection.Above"/>
    /// is each one mirrored. That is still one declaration serving two directions, but the mirroring now happens
    /// where a direction was explicitly asked for rather than silently inside an operator's declaration.
    /// </para>
    /// <para>
    /// <see cref="OrderingConfidence.Certain"/> is <c>aU &lt; bL</c>, <see cref="OrderingConfidence.Nominal"/> is
    /// <c>a &lt; b</c>, and <see cref="OrderingConfidence.Possible"/> is <c>aL &lt; bU</c>. All three are strict:
    /// comparison is tolerance-aware, so a non-strict variant would differ only on values already judged the
    /// same number.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="confidence"/> is <see cref="OrderingConfidence.Contradicted"/>, which is the absence of
    /// every rung rather than a rung of its own.
    /// </exception>
    public static ComparisonRule RuleFor(OrderingDirection direction, OrderingConfidence confidence)
    {
        var below = confidence switch
        {
            OrderingConfidence.Possible =>
                new ComparisonRule(Landmark.LowerBound, ComparisonType.LessThan, Landmark.UpperBound),
            OrderingConfidence.Nominal =>
                new ComparisonRule(Landmark.Nominal, ComparisonType.LessThan, Landmark.Nominal),
            OrderingConfidence.Certain =>
                new ComparisonRule(Landmark.UpperBound, ComparisonType.LessThan, Landmark.LowerBound),
            OrderingConfidence.Contradicted => throw new ArgumentOutOfRangeException(
                nameof(confidence), confidence, "Contradicted is the absence of every rung, not a rung."),
            _ => throw new ArgumentOutOfRangeException(
                nameof(confidence), confidence, "Unknown ordering tier."),
        };

        return direction switch
        {
            OrderingDirection.Below => below,
            OrderingDirection.Above => below.Mirrored,
            _ => throw new ArgumentOutOfRangeException(
                nameof(direction), direction, "Unknown ordering direction."),
        };
    }

    /// <summary>
    /// Which rung <paramref name="rule"/> is, or <see langword="null"/> where it is not one.
    /// </summary>
    /// <remarks>
    /// The discovery half, and the reason an operator can declare its comparison plainly and still be placed on
    /// the ladder. Null is the honest answer for the comparisons that are genuinely off it —
    /// <c>⌜&lt;⌝</c> and <c>⌞&gt;⌟</c> compare a derived <i>statistic</i> of each side, and no amount of
    /// strengthening or weakening a tier reaches them.
    /// </remarks>
    public static OrderingRung? RungOf(this ComparisonRule rule)
    {
        foreach (var rung in Rungs)
        {
            if (rung.Rule == rule) return rung;
        }

        return null;
    }

    /// <summary>
    /// The strongest tier <paramref name="lhs"/> and <paramref name="rhs"/> reach in
    /// <paramref name="direction"/>, or <see langword="null"/> where that cannot be settled.
    /// </summary>
    /// <remarks>
    /// Read from the top down and stops as soon as it knows, so a certainly-ordered pair costs one comparison
    /// rather than three. An unanswered rung ends the walk: it obscures the tiers at and above it, and nothing
    /// weaker can be reported as the strongest reached while a stronger one is unknown.
    /// </remarks>
    public static OrderingConfidence? AchievedTier(
        Measurand lhs, Measurand rhs, OrderingDirection direction)
    {
        for (var i = Strengthening.Length - 1; i >= 0; i--)
        {
            switch (RuleFor(direction, Strengthening[i]).IsSatisfiedGiven(lhs, rhs))
            {
                case true: return Strengthening[i];
                case null: return null;
            }
        }

        return OrderingConfidence.Contradicted;
    }

    /// <summary>Whether <paramref name="lhs"/> and <paramref name="rhs"/> reach at least <paramref name="rung"/>.</summary>
    /// <remarks>
    /// One comparison, because a tier <i>is</i> its rung. Deriving this from <see cref="AchievedTier"/> would
    /// needlessly answer "unknown" for a rung that is settled, merely because a stronger one is not.
    /// </remarks>
    public static bool? Reaches(Measurand lhs, Measurand rhs, OrderingRung rung) =>
        rung.Confidence is OrderingConfidence.Contradicted
            ? true
            : rung.Rule.IsSatisfiedGiven(lhs, rhs);
}
