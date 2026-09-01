using Calcusystem.DimensionedExpression.State;

namespace Calcusystem.DimensionedExpression.Enums;

/// <summary>Which n-ary expression a <see cref="NaryExpressionState"/> rebuilds into.</summary>
public enum NaryExpressionKind
{
    /// <summary>Product over its factors.</summary>
    Product,

    /// <summary>Sum over its addends.</summary>
    Sum,
}
