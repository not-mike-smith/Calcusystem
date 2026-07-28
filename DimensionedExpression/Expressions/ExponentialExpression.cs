using System;
using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement;
using Measurement.Exceptions;
using Measurement.Models;
using Measurement.Uncertainty;

namespace DimensionedExpression.Expressions;

/// <summary>
/// Unary <c>e^x</c> over a dimensionless <see cref="IExpression"/>. The argument must be dimensionless (enforced
/// on construction and assignment) and the result is dimensionless. Uncertainty: because <c>d(eˣ)/eˣ = dx</c>,
/// RelativeError(eˣ) ≈ |x|·RelativeError(x) (i.e. the absolute error of x).
/// </summary>
public class ExponentialExpression : IdBase, IExpression
{
    private IExpression _argument;

    public ExponentialExpression(IExpression argument, string id = Constants.CREATE_NEW) : base(id)
    {
        RequireDimensionless(argument);
        _argument = argument;
    }

    public IExpression Argument
    {
        get => _argument;
        set
        {
            RequireDimensionless(value);
            _argument = value;
        }
    }

    public bool IsDirectlyMutable => false;
    public bool IsFullyDescribed => Argument.IsFullyDescribed;
    public Dimensionality Dimensionality => Dimensionality.Dimensionless;

    public Measurand? Value
    {
        get
        {
            if (IsFullyDescribed is false) return null;

            var argument = Argument.Value!;
            var x = argument.KmsValue;
            var relativeError = Math.Abs(x) * argument.RelativeError;

            return Dimensionality.Dimensionless
                .Quantity(Math.Exp(x))
                .Measurand(GaussianUncertainty.FromRelErr(relativeError));
        }
    }

    public override string ToString()
    {
        return $"exp({Argument})";
    }

    public int DegreesOfFreedom()
    {
        return Argument.DegreesOfFreedom();
    }

    private static void RequireDimensionless(IExpression argument)
    {
        if (argument.Dimensionality != Dimensionality.Dimensionless)
            throw new IncompatibleDimensionsException(
                $"ExponentialExpression argument must be dimensionless, was {argument.Dimensionality}");
    }
}
