using Calcusystem.Measurement.Comparison;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Quantities;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// One comparison between a landmark of the left value and a landmark of the right — the atom every binary
/// operator is built from.
/// </summary>
/// <remarks>
/// <para>
/// Each of the thirteen operators used to hand-write its own interval arithmetic, and every one of them turned
/// out to be a conjunction of comparisons between one of the subject's three landmarks and one of the
/// criterion's. Declaring that conjunction rather than writing it means an operator can no longer disagree with
/// its own documentation, and the comparison itself happens in exactly one place —
/// <see cref="MeasurandComparer"/> — which is where tolerance, dimensional mismatch and non-finite values are
/// already handled.
/// </para>
/// <para>
/// <b>Pure, and deliberately not an <c>IBinaryOperator</c>.</b> A rule has no identity, no operands and no
/// provenance; composing operators out of child operators would duplicate all three and force the wire format to
/// carry them twice. An operator holds rules; a rule holds nothing.
/// </para>
/// <para>
/// Both landmarks are named independently, so all nine landmark pairs are expressible — including the ones no
/// named operator asks for. That is what lets <c>SimpleComparison</c> offer spellings such as "my nominal is
/// above your guaranteed floor" without a class per spelling.
/// </para>
/// </remarks>
/// <param name="Lhs">Which landmark of the left value is compared.</param>
/// <param name="Type">Which outcomes count as satisfying the rule.</param>
/// <param name="Rhs">Which landmark of the right value it is compared against.</param>
public readonly record struct ComparisonRule(Landmark Lhs, ComparisonType Type, Landmark Rhs)
{
    /// <summary>
    /// Whether this rule holds for the two values supplied, or <see langword="null"/> when the comparison has no
    /// answer.
    /// </summary>
    /// <remarks>
    /// Null is <see cref="ComparisonResult.Incomparable"/> reaching the caller intact — different dimensions, a
    /// <see cref="double.NaN"/>, or two same-signed infinities. It must not collapse to <see langword="false"/>:
    /// a rule that cannot be evaluated has not been violated, and reporting it as violated would manufacture a
    /// finding out of a missing answer.
    /// </remarks>
    public bool? IsSatisfiedGiven(Measurand lhs, Measurand rhs)
    {
        var result = MeasurandComparer.Compare(lhs, Lhs, rhs, Rhs);

        return result is ComparisonResult.Incomparable ? null : (result & (ComparisonResult)Type) != 0;
    }

    /// <summary>
    /// The same claim made about the operands the other way round: <c>rule.Mirrored</c> holds for
    /// <c>(a, b)</c> exactly when <c>rule</c> holds for <c>(b, a)</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both landmarks swap sides and the relation reverses, so <c>⌜&lt;⌟</c> — my ceiling below your floor —
    /// mirrors to <c>⌞&gt;⌝</c>, my floor above your ceiling. Equality is untouched, being its own reverse.
    /// </para>
    /// <para>
    /// This is what lets each mirrored pair of operators be declared once. Writing the greater-than family as
    /// its own literal triples would work, but it would leave the mirror relationship as a coincidence between
    /// two hand-written declarations rather than something the code states and a test can check.
    /// </para>
    /// </remarks>
    public ComparisonRule Mirrored => new(Rhs, Reverse(Type), Lhs);

    /// <summary>
    /// This rule written in the operator notation — the left glyph, the relation, the right glyph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generated rather than declared, which is the test of whether the notation in <c>OPERATORS.md</c> is
    /// actually systematic: the bar picks the statistic (top for a ceiling, bottom for a floor, a mid dot for the
    /// reported value) and the corner opens toward the operator, so <c>⌜&lt;⌟</c> reads "my ceiling is below
    /// your floor". The six ordering operators declare their symbols by hand and
    /// <c>ComparisonRuleTests</c> asserts the generated ones match, so the alphabet cannot drift from the
    /// operators it describes.
    /// </para>
    /// <para>
    /// Compound operators keep hand-written symbols. <c>·=}</c> is a band, not a comparison, and spelling it as
    /// its two rules would lose the thing the notation exists to convey.
    /// </para>
    /// </remarks>
    public string Symbol => $"{LeftGlyph(Lhs)}{RelationGlyph(Type)}{RightGlyph(Rhs)}";

    public override string ToString() => Symbol;

    /// <summary>
    /// Whether every rule holds, three-valued: <see langword="false"/> if any is violated,
    /// <see langword="null"/> if none is violated but some could not be answered, otherwise
    /// <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Kleene conjunction, and the precedence of <see langword="false"/> over <see langword="null"/> is the
    /// point: one rule definitively failing settles the conjunction whatever the others could not answer. An
    /// empty set is vacuously satisfied; no operator declares one.
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
    private static ComparisonType Reverse(ComparisonType type)
    {
        var reversed = type & ComparisonType.EqualTo;

        if (type.HasFlag(ComparisonType.LessThan)) reversed |= ComparisonType.GreaterThan;
        if (type.HasFlag(ComparisonType.GreaterThan)) reversed |= ComparisonType.LessThan;

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

    private static string RelationGlyph(ComparisonType type) => type switch
    {
        ComparisonType.None => "∅",
        ComparisonType.GreaterThan => ">",
        ComparisonType.LessThan => "<",
        ComparisonType.InequalTo => "≠",
        ComparisonType.EqualTo => "=",
        ComparisonType.GreaterThanOrEqualTo => "≥",
        ComparisonType.LessThanOrEqualTo => "≤",
        ComparisonType.Any => "?",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown comparison type."),
    };
}
