namespace Calcusystem.DimensionedExpression.Exceptions;

/// <summary>A node was given a name that <see cref="Names.NodeName"/> refuses.</summary>
public sealed class InvalidNameException(string message) : ArgumentException(message);
