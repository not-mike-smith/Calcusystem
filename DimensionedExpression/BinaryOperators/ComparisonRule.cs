using Calcusystem.Measurement.Comparison;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// One comparison between a landmark of the left value and a landmark of the right — the atom every binary
/// operator is built from.
/// </summary>
/// <remarks>
/// <para>
/// Every operator states its rules rather than writing interval arithmetic, and every one of them is a
/// conjunction of comparisons between one of the subject's three landmarks and one of the criterion's.
/// Declaring the conjunction rather than writing it keeps the comparison itself in exactly one place —
/// <see cref="MeasurandComparer"/> — which is where tolerance, dimensional mismatch and non-finite values are
/// handled.
/// </para>
/// <para>
/// A rule has no identity, no operands and no provenance: an operator holds rules, a rule holds nothing.
/// </para>
/// <para>
/// Both landmarks are named independently, so every landmark pair is expressible — including the ones no named
/// operator asks for. That is what lets <c>SimpleComparison</c> offer spellings such as "my nominal is above
/// your guaranteed floor" without a class per spelling.
/// </para>
/// </remarks>
/// <param name="Lhs">Which landmark of the left value is compared.</param>
/// <param name="MustBe">Which outcomes count as satisfying the rule.</param>
/// <param name="Rhs">Which landmark of the right value it is compared against.</param>
public readonly record struct ComparisonRule(Landmark Lhs, MustBe MustBe, Landmark Rhs)
{
    /// <summary>
    /// Whether this rule holds for the two values supplied, or <see langword="null"/> when the comparison has no
    /// answer.
    /// </summary>
    /// <remarks>
    /// Null is <see cref="ComparisonResult.Incomparable"/> reaching the caller intact — different dimensions, a
    /// <see cref="double.NaN"/>, or two same-signed infinities. It must not collapse to <see langword="false"/>:
    /// a rule that cannot be evaluated has not been violated.
    /// </remarks>
    public bool? IsSatisfiedGiven(Measurand lhs, Measurand rhs)
    {
        var result = MeasurandComparer.Compare(lhs, Lhs, rhs, Rhs);

        return result is ComparisonResult.Incomparable ? null : (result & (ComparisonResult)MustBe) != 0;
    }

    /// <summary>
    /// The same claim made about the operands the other way round: <c>rule.Mirrored</c> holds for
    /// <c>(a, b)</c> exactly when <c>rule</c> holds for <c>(b, a)</c>.
    /// </summary>
    /// <remarks>
    /// Both landmarks swap sides and the relation reverses, so <c>⌜&lt;⌟</c> — my ceiling below your floor —
    /// mirrors to <c>⌞&gt;⌝</c>, my floor above your ceiling. Equality is untouched, being its own reverse.
    /// </remarks>
    public ComparisonRule Mirrored => new(Rhs, Reverse(MustBe), Lhs);

    /// <summary>
    /// This rule written in the operator notation — the left glyph, the relation, the right glyph.
    /// </summary>
    /// <remarks>
    /// The bar picks the statistic (top for a ceiling, bottom for a floor, a mid dot for the reported value) and
    /// the corner opens toward the operator, so <c>⌜&lt;⌟</c> reads "my ceiling is below your floor". Compound
    /// operators keep hand-written symbols instead. See <c>OPERATORS.md</c> for the alphabet.
    /// </remarks>
    public string Symbol => $"{LeftGlyph(Lhs)}{RelationGlyph(MustBe)}{RightGlyph(Rhs)}";

    public override string ToString() => Symbol;

    /// <summary>
    /// Whether every rule holds, three-valued: <see langword="false"/> if any is violated,
    /// <see langword="null"/> if none is violated but some could not be answered, otherwise
    /// <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Kleene conjunction: <see langword="false"/> beats <see langword="null"/>, so one rule definitively
    /// failing settles the conjunction whatever the others could not answer. An empty set is vacuously
    /// satisfied; no operator declares one.
    /// </remarks>
    public static bool? AllSatisfied(IReadOnlyList<ComparisonRule> rules, Measurand lhs, Measurand rhs)
    {
        var anyUnanswered = false;

        foreach (var rule in rules)
        {
            switch (rule.IsSatisfiedGiven(lhs, rhs))
            {
                case false: return false;
                case null: anyUnanswered = true; break;
            }
        }

        return anyUnanswered ? null : true;
    }

    /// <summary>The mask accepting the reverse of everything <paramref name="type"/> accepts.</summary>
    /// <remarks>
    /// The two ordering bits trade places; the equality bit stays, since agreement reads the same from either
    /// side. As a mask this is a permutation, so <c>≤</c> reverses to <c>≥</c> and <c>≠</c> to itself with no
    /// case analysis beyond the two bits.
    /// </remarks>
    private static MustBe Reverse(MustBe type)
    {
        var reversed = type & MustBe.EqualTo;

        if (type.HasFlag(MustBe.LessThan)) reversed |= MustBe.GreaterThan;
        if (type.HasFlag(MustBe.GreaterThan)) reversed |= MustBe.LessThan;

        return reversed;
    }

    private static string LeftGlyph(Landmark landmark) => landmark switch
    {
        Landmark.LowerBound => "⌞",
        Landmark.Nominal => "·",
        Landmark.UpperBound => "⌜",
        _ => throw new ArgumentOutOfRangeException(nameof(landmark), landmark, "Unknown landmark."),
    };

    private static string RightGlyph(Landmark landmark) => landmark switch
    {
        Landmark.LowerBound => "⌟",
        Landmark.Nominal => "·",
        Landmark.UpperBound => "⌝",
        _ => throw new ArgumentOutOfRangeException(nameof(landmark), landmark, "Unknown landmark."),
    };

    private static string RelationGlyph(MustBe type) => type switch
    {
        MustBe.Impossible => "∅",
        MustBe.GreaterThan => ">",
        MustBe.LessThan => "<",
        MustBe.UnequalTo => "≠",
        MustBe.EqualTo => "=",
        MustBe.GreaterThanOrEqualTo => "≥",
        MustBe.LessThanOrEqualTo => "≤",
        MustBe.Comparable => "?",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown comparison type."),
    };
}
