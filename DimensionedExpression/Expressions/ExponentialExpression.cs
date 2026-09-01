using System;
using Calcusystem.Core.Identity;
using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Measurement.Dimensions;
using Calcusystem.Measurement.Exceptions;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Quantities;
using Calcusystem.Measurement.Uncertainties;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Unary <c>e^x</c> over a dimensionless <see cref="IExpression"/>. The argument must be dimensionless (enforced
/// on construction, which is the only point it can be supplied) and the result is dimensionless.
/// <br/>
/// Uncertainty: because <c>d(eˣ)/eˣ = dx</c>,
/// RelativeError(eˣ) ≈ |x|·RelativeError(x) (i.e. the absolute error of x).
/// </summary>
public class ExponentialExpression : ExpressionBase, IExpression, IStatefulNode<ExponentialExpression, UnaryExpressionState>
{
    private readonly IExpression _argument;

    public ExponentialExpression(IExpression argument, string id = Constants.CREATE_NEW_ID) : base(id)
    {
        RequireDimensionless(argument);
        _argument = argument;
    }

    public IExpression Argument => _argument;

    public override bool IsDirectlyMutable => false;
    public override bool IsFullyDescribed => Argument.IsFullyDescribed;
    public override Dimensionality Dimensionality => Dimensionality.Dimensionless;

    /// <inheritdoc/>
    public override Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IErrorPropagator? propagator = null)
    {
        if (! known.TryGetValue(Argument, out var argument)) return null;

        var x = argument.KmsValue;
        var relativeError = Math.Abs(x) * argument.RelativeError;

        return Dimensionality.Dimensionless
            .Quantity(Math.Exp(x))
            .Measurand(SymmetricUncertainty.FromRelErr(relativeError));
    }

    public override string ToString()
    {
        return $"exp({Argument})";
    }

    /// <inheritdoc/>
    public override IEnumerable<IExpression> Children => [Argument];

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
