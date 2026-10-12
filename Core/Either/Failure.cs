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
/// It carries one message, not a list of findings. Rich domain output — every dimensional problem in a model,
/// every violated requirement in a calculation — stays its own type, because a caller wants to read it rather
/// than branch on it. This says only that an operation did not produce a value, and why.
/// </para>
/// </remarks>
/// <param name="Reason">What sort of failure this is.</param>
/// <param name="Message">What went wrong, in a form fit to show someone.</param>
public sealed record Failure(FailureReason Reason, string Message)
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
