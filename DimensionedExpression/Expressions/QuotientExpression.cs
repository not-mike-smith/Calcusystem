using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement.BaseClasses;
using Measurement.Models;

namespace DimensionedExpression.Expressions;

public class QuotientExpression : CalculatedExpressionBase, ICalculatedExpression
{
    public required IExpression Numerator { get; set; }

    public required IExpression Denominator { get; set; }

    public bool IsFullyDescribed => Numerator.IsFullyDescribed && Denominator.IsFullyDescribed;
    public Dimensionality Dimensionality => Numerator.Dimensionality / Denominator.Dimensionality;

    public PrecisionQuantity? Value => IsFullyDescribed
        ? PrecisionQuantity.Quotient(ErrorPropagation, Numerator.Value!, Denominator.Value!)
        : null;

    public override string ToString()
    {
        return $"{Numerator} / {Denominator}";
    }

    public int DegreesOfFreedom()
    {
        throw new NotImplementedException();
    }
}
