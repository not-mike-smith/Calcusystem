using Calcusystem.Core.Enums;

namespace Calcusystem.Core.Either;

/// <summary>
/// Why something did not work: a reason a caller can branch on, and a message a person can read.
/// </summary>
/// <remarks>
/// <para>
/// Not generic, which is what lets a failure travel. A service that fails returns this, and a caller propagating
/// it upward through a different payload type hands the same instance on rather than rebuilding it — see
/// <see cref="Either{TValue}"/>.
/// </para>
/// <para>
/// A <see cref="Reason"/> and a message are all every caller can be expected to understand, so they are all
/// that is required. Where a failure has more to say — the list of problems that stopped a model from
/// building, say — <see cref="Failure{TDetail}"/> carries it without any of this changing.
/// </para>
/// </remarks>
/// <param name="Reason">What sort of failure this is.</param>
/// <param name="Message">What went wrong, in a form fit to show someone.</param>
public record Failure(FailureReason Reason, string Message)
{
    /// <summary>This failure as the outcome of an operation that would have produced a <typeparamref name="TValue"/>.</summary>
    /// <typeparam name="TValue">The value type the caller's own signature promises.</typeparam>
    /// <remarks>
    /// The explicit form of the implicit conversion on <see cref="Either{TValue}"/>, for where type inference
    /// cannot see what is wanted — a ternary, or a lambda whose return type is not stated.
    /// </remarks>
    public Either<TValue> AsType<TValue>() => new Failed<TValue>(this);

    public override string ToString() => $"{Reason}: {Message}";
}

/// <summary>
/// A failure with structured detail attached — the findings behind it, for a caller equipped to read them.
/// </summary>
/// <remarks>
/// <para>
/// Subtyping rather than a second type parameter on <see cref="Either{TValue}"/>, so a caller that only wants
/// to know whether something failed, or to relay it, needs to know nothing about the detail. Everything that
/// takes a <see cref="Failure"/> takes one of these.
/// </para>
/// <para>
/// <b>Relaying preserves it.</b> <see cref="Failure.AsType{TValue}"/> hands on the same instance, so detail
/// attached deep in a call stack is still there at the top, recovered by testing for this type:
/// <code>
/// if (failure is Failure&lt;IReadOnlyList&lt;DraftProblem&gt;&gt; detailed) Render(detailed.Detail);
/// </code>
/// </para>
/// <para>
/// The cost of this shape over a second type parameter: a signature returning <c>Either&lt;T&gt;</c> does not
/// advertise what its failures carry. Where that matters, say so in the member's own documentation.
/// </para>
/// </remarks>
/// <param name="Reason">What sort of failure this is.</param>
/// <param name="Message">What went wrong, in a form fit to show someone.</param>
/// <param name="Detail">The findings behind the failure.</param>
/// <typeparam name="TDetail">What the detail is.</typeparam>
public sealed record Failure<TDetail>(FailureReason Reason, string Message, TDetail Detail)
    : Failure(Reason, Message)
{
    public override string ToString() => $"{Reason}: {Message} ({Detail})";
}
