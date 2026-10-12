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
/// <b>There is deliberately no <c>IsSuccess</c> and no <c>Value</c> here.</b> Either would be answerable on a
/// failure, and a caller would reach the value by casting — which is the runtime exception this type exists to
/// replace. The value is reachable only from the case that has one: through <see cref="Match{TOut}"/>, or by
/// matching <see cref="Succeeded{TValue}"/>.
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

    /// <summary>Reads a plain value as a success.</summary>
    /// <param name="value">What the operation produced.</param>
    public static implicit operator Either<TValue>(TValue value) => new Succeeded<TValue>(value);

    /// <summary>Reads a failure as the outcome of an operation of this value type.</summary>
    /// <param name="failure">Why it did not work.</param>
    public static implicit operator Either<TValue>(Failure failure) => new Failed<TValue>(failure);
}
