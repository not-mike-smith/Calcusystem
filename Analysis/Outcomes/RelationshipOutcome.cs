using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Analysis.Outcomes;

/// <summary>
/// What one relationship did in a calculation: whether it held, and the two values it was judged on.
/// </summary>
/// <remarks>
/// <para>
/// Every relationship the system carries gets one of these, including the ones that could not be judged. A
/// silently absent verdict is the failure mode worth designing against — an engineer scanning a clean report
/// should be able to tell "the check passed" from "the check never ran", and only listing outcomes for the
/// relationships that resolved makes those two indistinguishable.
/// </para>
/// <para>
/// The two values are stored <i>positionally</i>, as <see cref="Lhs"/> and <see cref="Rhs"/>, because that is
/// what was compared and what the operator's predicate was handed. <see cref="Subject"/> and
/// <see cref="Criterion"/> are views over them, so the presentation and the arithmetic cannot drift apart.
/// </para>
/// <para>
/// Everything on this record is a <i>value</i>; everything on <see cref="Relationship"/> is an
/// <i>expression</i>. So <c>outcome.Criterion</c> is what the bound worked out to and
/// <c>outcome.Relationship.Criterion</c> is the expression that produced it.
/// </para>
/// </remarks>
/// <param name="Relationship">The relationship this is an outcome for.</param>
/// <param name="IsSatisfied">
/// Whether it held: <see langword="true"/> / <see langword="false"/>, or <see langword="null"/> when either
/// side did not resolve, in which case the relationship is outstanding rather than passing.
/// </param>
/// <param name="Lhs">The value of the left operand, or null if it did not resolve.</param>
/// <param name="Rhs">The value of the right operand, or null if it did not resolve.</param>
public sealed record RelationshipOutcome(
    IBinaryOperator Relationship,
    bool? IsSatisfied,
    Measurand? Lhs,
    Measurand? Rhs)
{
    /// <summary>
    /// The value that was judged, or null where the relationship judges neither side or that side did not
    /// resolve. See <see cref="IBinaryOperator.Subject"/>.
    /// </summary>
    public Measurand? Subject => Relationship.Subject is null ? null : Lhs;

    /// <summary>
    /// The value it was judged against, or null where the relationship has no criterion or that side did not
    /// resolve. See <see cref="IBinaryOperator.Criterion"/>.
    /// </summary>
    public Measurand? Criterion => Relationship.Criterion is null ? null : Rhs;

    /// <summary>
    /// Whether this relationship judges one side against the other, which is what separates a
    /// <see cref="IsViolation"/> from an <see cref="IsInconsistency"/>.
    /// </summary>
    public bool HasCriterion => Relationship.Criterion is not null;

    /// <summary>
    /// A requirement that did not hold — a value fell outside a bound it was tested against. The model is
    /// coherent; the design or the measurement is out of specification.
    /// </summary>
    public bool IsViolation => IsSatisfied is false && HasCriterion;

    /// <summary>
    /// An equation or coherence assertion that did not hold. There is no criterion here, so nothing says which
    /// side is at fault — the finding is that two things that should agree do not, which points at the model or
    /// the inputs rather than at either operand.
    /// </summary>
    public bool IsInconsistency => IsSatisfied is false && ! HasCriterion;

    /// <summary>
    /// Neither satisfied nor violated: at least one side did not resolve, so the check is still outstanding.
    /// </summary>
    public bool IsUndetermined => IsSatisfied is null;

    public override string ToString() =>
        $"{Relationship} => {IsSatisfied?.ToString() ?? "unknown"}";
}
