namespace Calcusystem.DimensionedExpression.Enums;

/// <summary>Which way round an ordering claim runs.</summary>
/// <remarks>
/// Named rather than left implicit. The greater-than family is the less-than family mirrored, and encoding that
/// as "everything is a less-than unless the declaration says <c>.Mirrored</c>" made the direction of an operator
/// something you had to already know to read — so it is a parameter now, and every rung names its own.
/// </remarks>
public enum OrderingDirection : byte
{
    /// <summary>The subject is claimed to be below the criterion.</summary>
    Below = 1,

    /// <summary>The subject is claimed to be above the criterion.</summary>
    Above = 2,
}
