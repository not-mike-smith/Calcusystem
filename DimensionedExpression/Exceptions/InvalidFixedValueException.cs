namespace Calcusystem.DimensionedExpression.Exceptions;

/// <summary>A fixed value was given a value that <see cref="Expressions.FixedValueRule"/> refuses for its kind.</summary>
public sealed class InvalidFixedValueException(string message) : ArgumentException(message);
