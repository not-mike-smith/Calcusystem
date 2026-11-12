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
/// A value has no uncertainty when its uncertainty is zero whatever the value. A relative uncertainty on a
/// value of zero is still an uncertainty, though its absolute bounds there are zero.
/// </remarks>
public static class FixedValueRule
{
    /// <summary>
    /// Why <paramref name="value"/> is not a value a <paramref name="kind"/> accepts, or null if it is one.
    /// </summary>
    public static string? Problem(FixedExpressionType kind, Measurand value)
    {
        // Asked at a nominal value of one, where a relative and an absolute magnitude both read as themselves.
        // Asked at the value itself, a relative uncertainty on zero would read as none.
        var exact = value.Uncertainty.UpperAbsoluteUncertainty(1) == 0
                    && value.Uncertainty.LowerAbsoluteUncertainty(1) == 0;

        return kind switch
        {
            FixedExpressionType.Constant when ! exact =>
                "A constant has no uncertainty. A value with uncertainty is a reference value.",

            FixedExpressionType.ReferenceValue when exact =>
                "A reference value has uncertainty. A value with none is a constant.",

            FixedExpressionType.Scalar when ! exact => "A scalar has no uncertainty.",

            FixedExpressionType.Scalar when value.Dimensionality != Dimensionality.Dimensionless =>
                $"A scalar is dimensionless, and this value is {value.Dimensionality}.",

            _ => null,
        };
    }
}
