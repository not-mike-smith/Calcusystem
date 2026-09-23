using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// A relationship asserting one <see cref="ComparisonRule"/> — any landmark of the subject against any landmark
/// of the criterion, at any strictness.
/// </summary>
/// <remarks>
/// <para>
/// The general form of the ordering family.
/// <b>It deliberately overlaps the named types.</b> Configured with the nominal-against-nominal rule it is
/// <c>NominallyLessThanOperator</c> in every respect including its symbol — the two spell the same relation.
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
    /// <see cref="MustBe.Impossible"/> is the only mask refused; it is the enum's zero, so refusing it turns a
    /// forgotten field into an error where it was forgotten. <see cref="MustBe.Comparable"/> is allowed — it
    /// asserts that both landmarks are well-defined quantities, which nothing else spells.
    /// </remarks>
    public SimpleComparison(ComparisonRule rule)
    {
        if (rule.MustBe is MustBe.Impossible)
        {
            throw new ArgumentException(
                "A comparison accepting no outcome can never be satisfied, and would report as a violation of "
                + "something the model never asserted.",
                nameof(rule));
        }

        Rule = rule;
        Rules = [rule];
    }

    protected override BinaryOperatorType Type => BinaryOperatorType.SimpleComparison;

    /// <summary>The single comparison this relationship asserts.</summary>
    public ComparisonRule Rule { get; }

    /// <summary><inheritdoc/></summary>
    /// <remarks>
    /// A rule is commutative exactly when mirroring leaves it unchanged — when its mask carries no ordering
    /// bias, so swapping the operands cannot change the answer. <c>·=·</c> and <c>·≠·</c> qualify; nothing with
    /// a <c>&lt;</c> or <c>&gt;</c> bit does, and neither does a rule comparing two different landmarks.
    /// </remarks>
    public override bool IsCommutative => Rule == Rule.Mirrored;

    /// <summary><inheritdoc/></summary>
    /// <remarks>
    /// Generated from the rule rather than declared, since there is no fixed relation to name. This is the one
    /// operator whose notation is computed end to end, and the reason the glyph alphabet had to be systematic.
    /// </remarks>
    public override string Symbol => Rule.Symbol;

    /// <inheritdoc/>
    public override IReadOnlyList<ComparisonRule> Rules { get; }

    /// <inheritdoc/>
    public override BinaryOperatorSnapshot GetSnapshot() => base.GetSnapshot() with { Rule = Rule };
}
