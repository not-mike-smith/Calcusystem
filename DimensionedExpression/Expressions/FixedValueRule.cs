using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>The rule for which value each kind of fixed value accepts.</summary>
/// <remarks>
/// <list type="bullet">
/// <item>A constant has no uncertainty.</item>
/// <item>A reference value has some.</item>
/// <item>A scalar has no uncertainty and is dimensionless.</item>
/// </list>
/// A value has no uncertainty when both of its absolute bounds are zero.
/// </remarks>
public static class FixedValueRule
{
    /// <summary>
    /// Why <paramref name="value"/> is not a value a <paramref name="kind"/> accepts, or null if it is one.
    /// </summary>
    public static string? Problem(FixedExpressionType kind, Measurand value) => throw new NotImplementedException();
}
