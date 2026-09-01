namespace Calcusystem.Measurement.Enums;

/// <summary>
/// Which <see cref="ComparisonResult"/>s a comparison will accept — a set, not a single relation.
/// </summary>
/// <remarks>
/// <para>
/// A mask over <see cref="ComparisonResult"/>'s bits, so asking whether a result is acceptable is
/// <c>(result &amp; type) != 0</c>. Every relation an engineer writes falls out of that with no special cases:
/// <c>≤</c> is the union of two outcomes rather than a relation of its own, and negation is complement against
/// <see cref="Any"/>.
/// </para>
/// <para>
/// <see cref="ComparisonResult.Incomparable"/> is zero, so it satisfies no mask — including
/// <see cref="Any"/> — which is exactly right: an unanswerable comparison is not accepted by an accept-anything
/// rule, it simply has no answer. Callers distinguish "rejected" from "unanswerable" by testing the result
/// against <see cref="ComparisonResult.Incomparable"/>, not by inspecting the mask.
/// </para>
/// <para>
/// Lives beside <see cref="ComparisonResult"/> deliberately. The two enums are one design — a mask whose bits
/// do not line up with the results it masks is silently wrong everywhere — so they are kept where a reader
/// changing either can see the other. <c>ComparisonTypeTests</c> pins the correspondence.
/// </para>
/// </remarks>
[Flags]
public enum ComparisonType : byte
{
    /// <summary>Accepts nothing. Never satisfied, and what <c>default</c> means.</summary>
    /// <remarks>
    /// Zero is the empty set rather than a relation, so a default-constructed rule is inert instead of
    /// quietly asserting equality.
    /// </remarks>
    None = 0b000,

    /// <summary>Accepts <see cref="ComparisonResult.GreaterThan"/>. Written <c>&gt;</c>.</summary>
    GreaterThan = 0b001,

    /// <summary>Accepts <see cref="ComparisonResult.LessThan"/>. Written <c>&lt;</c>.</summary>
    LessThan = 0b010,

    /// <summary>Accepts either strict ordering but not agreement. Written <c>≠</c>.</summary>
    InequalTo = LessThan | GreaterThan,

    /// <summary>Accepts <see cref="ComparisonResult.Equal"/>. Written <c>=</c>.</summary>
    EqualTo = 0b100,

    /// <summary>Accepts agreement or a greater value. Written <c>≥</c>.</summary>
    GreaterThanOrEqualTo = GreaterThan | EqualTo,

    /// <summary>Accepts agreement or a lesser value. Written <c>≤</c>.</summary>
    LessThanOrEqualTo = LessThan | EqualTo,

    /// <summary>Accepts any determinate outcome — satisfied whenever the comparison has an answer at all.</summary>
    Any = LessThan | GreaterThan | EqualTo,
}
