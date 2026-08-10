using Calcusystem.DimensionedExpression.State;
using Calcusystem.Core;
using System;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Exceptions;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Unary <c>e^x</c> over a dimensionless <see cref="IExpression"/>. The argument must be dimensionless (enforced
/// on construction and assignment) and the result is dimensionless.
/// <br/>
/// Uncertainty: because <c>d(eˣ)/eˣ = dx</c>,
/// RelativeError(eˣ) ≈ |x|·RelativeError(x) (i.e. the absolute error of x).
/// </summary>
public class ExponentialExpression : IdBase, IExpression, IStatefulNode<ExponentialExpression, UnaryExpressionState>
{
    private IExpression _argument;

    public ExponentialExpression(IExpression argument, string id = Constants.CREATE_NEW_ID) : base(id)
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
                .Measurand(SymmetricUncertainty.FromRelErr(relativeError));
        }
    }

    public override string ToString()
    {
        return $"exp({Argument})";
    }

    /// <inheritdoc/>
    public IEnumerable<IExpression> Children => [Argument];

    private static void RequireDimensionless(IExpression argument)
    {
        if (argument.Dimensionality != Dimensionality.Dimensionless)
            throw new IncompatibleDimensionsException(
                $"ExponentialExpression argument must be dimensionless, was {argument.Dimensionality}");
    }

    /// <inheritdoc/>
    public UnaryExpressionState GetState() =>
        new(UnaryExpressionKind.Exponential, Id, Argument.Id);

    /// <inheritdoc/>
    public static ExponentialExpression FromState(UnaryExpressionState state, INodeResolver resolve) =>
        new(resolve.Resolve<IExpression>(state.InnerId), state.Id);
}
