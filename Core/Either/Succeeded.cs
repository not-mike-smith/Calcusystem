namespace Calcusystem.Core.Either;

/// <summary>
/// An operation that produced a value.
/// </summary>
/// <remarks>
/// The one case that has a <see cref="Value"/>, which is why the value lives here and not on
/// <see cref="Either{TValue}"/>: a caller holding the base type cannot read it without first establishing that
/// there is one.
/// </remarks>
/// <param name="Value">What the operation produced.</param>
/// <typeparam name="TValue">What a successful operation produces.</typeparam>
public sealed record Succeeded<TValue>(TValue Value) : Either<TValue>
{
    /// <inheritdoc/>
    public override TOut Match<TOut>(Func<TValue, TOut> ifSucceeded, Func<Failure, TOut> ifFailed) =>
        ifSucceeded(Value);

    public override string ToString() => $"Succeeded: {Value}";
}
