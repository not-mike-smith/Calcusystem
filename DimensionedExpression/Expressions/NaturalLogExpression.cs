using System;
using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;
using Measurement;
using Measurement.Exceptions;
using Measurement.Models;
using Measurement.Uncertainty;

namespace DimensionedExpression.Expressions;

/// <summary>
/// Unary <c>ln(x)</c> over a dimensionless <see cref="IExpression"/>. The argument must be dimensionless
/// (enforced on construction and assignment) and, to be meaningful, positive; a non-positive value yields a
/// NaN or negative-infinity result. The result is dimensionless. Uncertainty: because <c>d(ln x) = dx/x</c>,
/// AbsoluteError(ln x) ≈ RelativeError(x).
/// </summary>
/// <remarks>
/// The uncertainty is inherently an absolute error, but the uncertainty types store a relative error, so it is
/// resolved as <c>RelativeError(x) / |ln x|</c>. When the argument is exactly 1 the result is 0 and that
/// relative error is undefined — the same degenerate case a sum that cancels to zero hits — and constructing the
/// result throws. Callers evaluating near <c>x = 1</c> should expect that edge.
/// </remarks>
public class NaturalLogExpression : IdBase, IExpression
{
    private IExpression _argument;

    public NaturalLogExpression(IExpression argument, string id = Constants.CREATE_NEW) : base(id)
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
            var absoluteError = argument.RelativeError; // AbsoluteError(ln x) ≈ RelativeError(x)

            return Dimensionality.Dimensionless
                .Quantity(Math.Log(argument.KmsValue))
                .Measurand(GaussianUncertainty.FromAbsErr(Dimensionality.Dimensionless.Quantity(absoluteError)));
        }
    }

    public override string ToString()
    {
        return $"ln({Argument})";
    }

    public int DegreesOfFreedom()
    {
        return Argument.DegreesOfFreedom();
    }

    private static void RequireDimensionless(IExpression argument)
    {
        if (argument.Dimensionality != Dimensionality.Dimensionless)
            throw new IncompatibleDimensionsException(
                $"NaturalLogExpression argument must be dimensionless, was {argument.Dimensionality}");
    }
}
