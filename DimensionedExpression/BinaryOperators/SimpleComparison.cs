using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// A relationship asserting one <see cref="ComparisonRule"/> — any landmark of the subject against any landmark
/// of the criterion, at any strictness.
/// </summary>
/// <remarks>
/// <para>
/// The general form of the ordering family. Six of these comparisons have named types because they are the ones
/// engineers reach for constantly; this covers the rest without a class apiece. "My reported value must stay
/// below your guaranteed floor" — <c>·&lt;⌟</c> — is an ordinary conservative acceptance criterion with no named
/// operator, and it is three bytes of state here.
/// </para>
/// <para>
/// <b>It deliberately overlaps the named types.</b> Configured with the nominal-against-nominal rule it is
/// <c>NominallyLessThanOperator</c> in every respect including its symbol, which is why the symbol-uniqueness
/// test excepts it: the two spell the same relation, so nothing is lost by a report that cannot tell them
/// apart. The named types stay because they are the ergonomic spelling and because the wire format identifies
/// operators by kind.
/// </para>
/// <para>
/// Always a <see cref="SolvingRole.Requirement"/>. An ordering confines a value to an interval rather than
/// producing one, so nothing can be derived from it however it is spelled.
/// </para>
/// <para>
/// Commutativity, by contrast, depends on the rule: <c>·=·</c> reads the same from either side while
/// <c>·&lt;·</c> does not. It is the only operator whose commutativity is not fixed by its type.
/// </para>
/// </remarks>
public class SimpleComparison : BinaryOperatorBase
{
    /// <param name="rule">The comparison this relationship asserts.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="rule"/> accepts no outcome at all — see the remarks.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <see cref="ComparisonType.None"/> is refused, and it is the only mask that is. A rule accepting no
    /// outcome is never satisfied, so it reports as a <i>violation</i> on every calculation — a finding against
    /// the model that the model never asserted, which is worse than useless because someone will go looking for
    /// it. And it is the enum's zero, so it is what an uninitialised mask reads as: refusing it here turns a
    /// forgotten field into an error at the point it was forgotten.
    /// </para>
    /// <para>
    /// <see cref="ComparisonType.Any"/> is <b>not</b> refused, though it looks like the same kind of mistake.
    /// Under a three-valued seam it is not vacuous: it answers <see langword="true"/> when the two landmarks can
    /// be compared and <see langword="null"/> when they cannot, so <c>⌜?⌝</c> asserts "both of these ceilings
    /// are well-defined quantities" — a real thing to want to check, and one nothing else spells.
    /// </para>
    /// </remarks>
    public SimpleComparison(ComparisonRule rule)
    {
        if (rule.Type is ComparisonType.None)
        {
            throw new ArgumentException(
                "A comparison accepting no outcome can never be satisfied, and would report as a violation of "
                + "something the model never asserted.",
                nameof(rule));
        }

        Rule = rule;
        Rules = [rule];
    }

    protected override BinaryOperatorKind Kind => BinaryOperatorKind.SimpleComparison;

    /// <summary>The single comparison this relationship asserts.</summary>
    public ComparisonRule Rule { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// A rule is commutative exactly when mirroring leaves it unchanged — when its mask carries no ordering
    /// bias, so swapping the operands cannot change the answer. <c>·=·</c> and <c>·≠·</c> qualify; nothing with
    /// a <c>&lt;</c> or <c>&gt;</c> bit does, and neither does a rule comparing two different landmarks.
    /// </remarks>
    public override bool IsCommutative => Rule == Rule.Mirrored;

    /// <inheritdoc/>
    /// <remarks>
    /// Generated from the rule rather than declared, since there is no fixed relation to name. This is the one
    /// operator whose notation is computed end to end, and the reason the glyph alphabet had to be systematic.
    /// </remarks>
    public override string Symbol => Rule.Symbol;

    /// <inheritdoc/>
    public override IReadOnlyList<ComparisonRule> Rules { get; }

    /// <inheritdoc/>
    public override BinaryOperatorState GetState() => base.GetState() with { Rule = Rule };
}
