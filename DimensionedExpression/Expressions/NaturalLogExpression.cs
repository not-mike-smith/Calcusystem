using Calcusystem.DimensionedExpression.State;
using Calcusystem.Core;
using System;
using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Exceptions;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Unary <c>ln(x)</c> over a dimensionless <see cref="IExpression"/>. The argument must be dimensionless
/// (enforced on construction and assignment) and, to be meaningful, positive; a non-positive value yields a
/// NaN or negative-infinity result. The result is dimensionless.
/// <br/>
/// Uncertainty: because <c>d(ln x) = dx/x</c>,
/// AbsoluteError(ln x) ≈ RelativeError(x).
/// </summary>
/// <remarks>
/// The uncertainty is inherently an absolute error and is stored as one (via <c>FromAbsErr</c>). At <c>x = 1</c>
/// the result is 0; its <em>relative</em> error is undefined, but the absolute error is retained and
/// <c>RelativeError</c> reports <c>+∞</c> rather than throwing.
/// </remarks>
public class NaturalLogExpression : ExpressionBase, IExpression, IStatefulNode<NaturalLogExpression, UnaryExpressionState>
{
    private IExpression _argument;

    public NaturalLogExpression(IExpression argument, string id = Constants.CREATE_NEW_ID) : base(id)
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

    public override bool IsDirectlyMutable => false;
    public override bool IsFullyDescribed => Argument.IsFullyDescribed;
    public override Dimensionality Dimensionality => Dimensionality.Dimensionless;


    /// <inheritdoc/>
    public override Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IErrorPropagator? propagator = null)
    {
        if (! known.TryGetValue(Argument, out var argument)) return null;

        var absoluteError = argument.RelativeError; // AbsoluteError(ln x) ≈ RelativeError(x)

        return Dimensionality.Dimensionless
            .Quantity(Math.Log(argument.KmsValue))
            .Measurand(SymmetricUncertainty.FromAbsErr(Dimensionality.Dimensionless.Quantity(absoluteError)));
    }

    public override string ToString()
    {
        return $"ln({Argument})";
    }

    /// <inheritdoc/>
    public override IEnumerable<IExpression> Children => [Argument];

    private static void RequireDimensionless(IExpression argument)
    {
        if (argument.Dimensionality != Dimensionality.Dimensionless)
            throw new IncompatibleDimensionsException(
                $"NaturalLogExpression argument must be dimensionless, was {argument.Dimensionality}");
    }

    /// <inheritdoc/>
    public UnaryExpressionState GetState() =>
        new(UnaryExpressionKind.NaturalLog, Id, Argument.Id);

    /// <inheritdoc/>
    public static NaturalLogExpression FromState(UnaryExpressionState state, INodeResolver resolve) =>
        new(resolve.Resolve<IExpression>(state.InnerId), state.Id);
}
