using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement.BaseClasses;
using Measurement.Models;

namespace DimensionedExpression.Expressions;

public class QuotientExpression : CalculatedExpressionBase, ICalculatedExpression
{
    private IExpression _numerator;
    private IExpression _denominator;

    public IExpression Numerator
    {
        get => _numerator;
        set => _numerator = value;
    }

    public IExpression Denominator
    {
        get => _denominator;
        set => _denominator = value;
    }
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
