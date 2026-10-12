namespace Calcusystem.Core.Either;

/// <summary>
/// An operation that did not produce a value, and why.
/// </summary>
/// <remarks>
/// Holds the <see cref="Failure"/> rather than copying its fields, so the same failure can be handed up through
/// a caller of a different value type — see <see cref="Either.Failure.AsType{TValue}"/>.
/// </remarks>
/// <param name="Failure">Why the operation did not produce a value.</param>
/// <typeparam name="TValue">What the operation would have produced.</typeparam>
public sealed record Failed<TValue>(Failure Failure) : Either<TValue>
{
    public override bool DidFail => true;

    /// <inheritdoc/>
    public override TOut Match<TOut>(Func<TValue, TOut> ifSucceeded, Func<Failure, TOut> ifFailed) =>
        ifFailed(Failure);

    public override string ToString() => $"Failed: {Failure}";
}
