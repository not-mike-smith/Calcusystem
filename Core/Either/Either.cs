using System.Diagnostics.CodeAnalysis;

namespace Calcusystem.Core.Either;

/// <summary>
/// Either a value or a <see cref="Failure"/>: what an operation that might not succeed returns, so that failing
/// needs no exception and succeeding needs no null.
/// </summary>
/// <remarks>
/// <para>
/// Two cases: <see cref="Succeeded{TValue}"/> and <see cref="Failed{TValue}"/>. Prefer
/// <see cref="Match{TOut}"/> to a <c>switch</c> over those types — <c>Match</c> is abstract, so it is total for
/// any case that can exist, whereas a type switch is exhaustive only for the two that exist today. The
/// parameterless constructor is inaccessible outside this assembly, which blocks the ordinary way of adding a
/// third case, but a record's copy constructor must stay <c>protected</c> (CS8878) and can be reached
/// explicitly, so the hierarchy is closed by convention rather than by the compiler.
/// </para>
/// <para>
/// <b>There is deliberately no <c>Value</c> here.</b> It would be answerable on a failure, and a caller would
/// reach it by casting — the runtime exception this type exists to replace. The value comes out only through
/// <see cref="Match{TOut}"/>, <see cref="TryGetValue"/>, or by matching <see cref="Succeeded{TValue}"/>.
/// </para>
/// <para>
/// <see cref="DidFail"/> is not the same hazard. A boolean cannot be used to reach a value it does not carry,
/// so asking the question costs nothing; it is <i>answering with a value that is not there</i> that had to go.
/// </para>
/// <para>
/// Both conversions below are implicit, so a method returning <c>Either&lt;T&gt;</c> can
/// <c>return someValue;</c> or <c>return someFailure;</c> and read like one that returns <c>T</c>.
/// </para>
/// </remarks>
/// <typeparam name="TValue">What a successful operation produces.</typeparam>
public abstract record Either<TValue>
{
    // Closes the hierarchy: derivable by the two cases beside it and by nothing outside this assembly.
    private protected Either() { }

    public abstract bool DidFail {get;}

    /// <summary>Answers with whichever of the two functions applies, so both cases must be handled.</summary>
    /// <typeparam name="TOut">What both branches produce.</typeparam>
    /// <param name="ifSucceeded">Given the value.</param>
    /// <param name="ifFailed">Given the failure.</param>
    public abstract TOut Match<TOut>(Func<TValue, TOut> ifSucceeded, Func<Failure, TOut> ifFailed);

    /// <summary>Runs an action for whichever case applies, where neither produces a value.</summary>
    /// <param name="ifSucceeded">Given the value.</param>
    /// <param name="ifFailed">Given the failure.</param>
    public void Switch(Action<TValue> ifSucceeded, Action<Failure> ifFailed) =>
        Match<object?>(
            value => { ifSucceeded(value); return null; },
            failure => { ifFailed(failure); return null; });

    /// <summary>Transforms the value if there is one, and passes a failure through untouched.</summary>
    /// <typeparam name="TOut">What <paramref name="transform"/> produces.</typeparam>
    /// <param name="transform">Applied to the value of a success.</param>
    /// <remarks>For a <paramref name="transform"/> that cannot itself fail. Use <see cref="Then{TOut}"/> when it can.</remarks>
    public Either<TOut> Map<TOut>(Func<TValue, TOut> transform) =>
        Match(value => (Either<TOut>)transform(value)!, failure => failure.AsType<TOut>());

    /// <summary>
    /// Continues with an operation that might itself fail, and passes an existing failure through untouched.
    /// </summary>
    /// <typeparam name="TOut">What the next operation produces.</typeparam>
    /// <param name="next">Applied to the value of a success.</param>
    /// <remarks>
    /// This is how dependent operations chain: the first failure short-circuits the rest, and nothing in between
    /// needs to test for it.
    /// </remarks>
    public Either<TOut> Then<TOut>(Func<TValue, Either<TOut>> next) =>
        Match(next, failure => failure.AsType<TOut>());

    /// <summary>The value if there is one, and <paramref name="fallback"/> if this failed.</summary>
    /// <param name="fallback">Used in place of a missing value.</param>
    public TValue ValueOr(TValue fallback) => Match(value => value, _ => fallback);

    /// <summary>Hands out the failure if there is one, for a caller that wants to give up early.</summary>
    /// <param name="failure">Why this failed, or null if it did not.</param>
    /// <returns>Whether this failed.</returns>
    /// <remarks>
    /// For relaying a dependency's failure without writing in a functional style. The failure converts
    /// implicitly, so the caller's own value type needs no mention:
    /// <code>
    /// var attempt = _dependency.DoSomething(parameters);
    /// if (attempt.TryGetFailure(out var failure)) return failure;
    /// </code>
    /// <see cref="DidFail"/> answers the same question where the failure itself is not wanted. There is no
    /// equivalent that hands out a <i>retyped</i> <see cref="Either{TValue}"/>, because it would have nothing to
    /// answer with on a success but an exception.
    /// </remarks>
    public bool TryGetFailure([NotNullWhen(true)] out Failure? failure)
    {
        failure = Match<Failure?>(_ => null, reason => reason);
        return failure is not null;
    }

    /// <summary>Hands out the value or the failure, whichever there is.</summary>
    /// <param name="value">What the operation produced, or null if it failed.</param>
    /// <param name="failure">Why it failed, or null if it did not.</param>
    /// <returns>Whether this succeeded.</returns>
    /// <remarks>
    /// The form for a caller that gives up early <i>and</i> then needs the value, with no cast on either path:
    /// <code>
    /// if (! attempt.TryGetValue(out var value, out var failure)) return failure;
    /// Use(value);
    /// </code>
    /// </remarks>
    public bool TryGetValue([NotNullWhen(true)] out TValue? value, [NotNullWhen(false)] out Failure? failure)
    {
        var (held, reason) = Match<(TValue?, Failure?)>(v => (v, null), f => (default, f));
        value = held;
        failure = reason;
        return failure is null;
    }

    /// <summary>Reads a plain value as a success.</summary>
    /// <param name="value">What the operation produced.</param>
    public static implicit operator Either<TValue>(TValue value) => new Succeeded<TValue>(value);

    /// <summary>Reads a failure as the outcome of an operation of this value type.</summary>
    /// <param name="failure">Why it did not work.</param>
    public static implicit operator Either<TValue>(Failure failure) => new Failed<TValue>(failure);
}
