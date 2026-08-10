using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Core;
using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.Measurement;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Binary quotient of a <see cref="Numerator"/> over a <see cref="Denominator"/> (both required); the result
/// dimensionality is the numerator's divided by the denominator's.
/// <br/>
/// A computed node: uncertainty is propagated through <see cref="Measurand"/> division using the
/// <see cref="ComputedExpressionBase.ErrorPropagation"/> method.
/// </summary>
public class QuotientExpression : ComputedExpressionBase, IComputedExpression, IStatefulNode<QuotientExpression, BinaryExpressionState>
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

    /// <inheritdoc/>
    public IEnumerable<IExpression> Children => [Numerator, Denominator];

    /// <inheritdoc/>
    public BinaryExpressionState GetState() =>
        new(BinaryExpressionKind.Quotient, Id, Numerator.Id, Denominator.Id, ErrorPropagation);

    /// <inheritdoc/>
    public static QuotientExpression FromState(BinaryExpressionState state, INodeResolver resolve) =>
        new()
        {
            Id = state.Id,
            Numerator = resolve.Resolve<IExpression>(state.InnerId1),
            Denominator = resolve.Resolve<IExpression>(state.InnerId2),
            ErrorPropagation = state.ErrorPropagation,
        };
}
