using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement;
using Measurement.Models;

namespace DimensionedExpression.Expressions;

public class QuotientExpression : CalculatedExpressionBase, ICalculatedExpression
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
