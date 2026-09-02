
namespace Calcusystem.DimensionedExpression.Enums;

/// <summary>Which n-ary expression a <see cref="NaryExpressionSnapshot"/> rebuilds into.</summary>
public enum NaryExpressionType
{
    /// <summary>Product over its factors.</summary>
    Product,

    /// <summary>Sum over its addends.</summary>
    Sum,
}
