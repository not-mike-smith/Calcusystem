using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement;
using Measurement.Models;

namespace DimensionedExpression.Expressions;

/// <summary>
/// Binary quotient of a <see cref="Numerator"/> over a <see cref="Denominator"/> (both required); the result
/// dimensionality is the numerator's divided by the denominator's.
/// <br/>
/// A computed node: uncertainty is propagated through <see cref="Measurand"/> division using the
/// <see cref="ComputedExpressionBase.ErrorPropagation"/> method.
/// </summary>
public class QuotientExpression : ComputedExpressionBase, IComputedExpression
{
    public required IExpression Numerator { get; set; }

    public required IExpression Denominator { get; set; }

    public bool IsFullyDescribed => Numerator.IsFullyDescribed && Denominator.IsFullyDescribed;
    public Dimensionality Dimensionality => Numerator.Dimensionality / Denominator.Dimensionality;

    public Measurand? Value => IsFullyDescribed
        ? Numerator.Value!.DividedBy(Denominator.Value!, ErrorPropagation)
        : null;

    public override string ToString()
    {
        return $"{Numerator} / {Denominator}";
    }

    public int DegreesOfFreedom()
    {
        return Numerator.DegreesOfFreedom() + Denominator.DegreesOfFreedom();
    }
}
