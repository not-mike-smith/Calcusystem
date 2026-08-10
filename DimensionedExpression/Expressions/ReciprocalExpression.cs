using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Core;
using Calcusystem.Measurement;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Unary reciprocal (<c>1/x</c>) of any <see cref="IExpression"/> (its <see cref="Reciprocand"/>); the result
/// dimensionality is the reciprocand's inverted (e.g. t → t⁻¹).
/// <br/>
/// Not directly mutable; <see cref="Value"/> is null until the reciprocand is fully described.
/// </summary>
public class ReciprocalExpression : IdBase, IExpression, IStatefulNode<ReciprocalExpression, UnaryExpressionState>
{
    private IExpression _reciprocand;

    public ReciprocalExpression(IExpression reciprocand, string id = Constants.CREATE_NEW_ID) : base(id)
    {
        _reciprocand = reciprocand;
    }

    public IExpression Reciprocand
    {
        get => _reciprocand;
        set => _reciprocand = value;
    }

    public bool IsDirectlyMutable => false;
    public bool IsFullyDescribed => Reciprocand.IsFullyDescribed;
    public Dimensionality Dimensionality => Reciprocand.Dimensionality.Reciprocal();

    public Measurand? Value => IsFullyDescribed
        ? Reciprocand.Value!.Reciprocal()
        : null;

    public override string ToString()
    {
        return $"1/({Reciprocand})";
    }

    /// <inheritdoc/>
    public IEnumerable<IExpression> Children => [Reciprocand];


    /// <inheritdoc/>
    public UnaryExpressionState GetState() =>
        new(UnaryExpressionKind.Reciprocal, Id, Reciprocand.Id);

    /// <inheritdoc/>
    public static ReciprocalExpression FromState(UnaryExpressionState state, INodeResolver resolve) =>
        new(resolve.Resolve<IExpression>(state.InnerId), state.Id);
}
