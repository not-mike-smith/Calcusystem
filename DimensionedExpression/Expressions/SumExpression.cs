using DimensionedExpression.Interfaces;
using DimensionedExpression.State;
using Calcusystem.Core;
using DimensionedExpression.BaseModels;
using Measurement;
using Measurement.Exceptions;

namespace DimensionedExpression.Expressions;

/// <summary>
/// N-ary sum (<c>+</c>) over its <see cref="Addends"/>, which must all share a dimensionality (enforced on
/// <see cref="AddAddend"/>; the constructor can seed a fixed dimensionality for an otherwise-empty sum).
/// <br/>
/// A computed node: uncertainty is propagated through <see cref="Measurand"/> addition using the
/// <see cref="ComputedExpressionBase.ErrorPropagation"/> method.
/// </summary>
public class SumExpression : ComputedExpressionBase, IComputedExpression, IStatefulNode<SumExpression, NaryExpressionState>
{
    private readonly List<IExpression> _addends = new();

    public SumExpression(Dimensionality dimensionality)
    {
        Dimensionality = dimensionality;
    }

    public SumExpression(IEnumerable<IExpression> addends)
    {
        _addends = addends.ToList();
        if (_addends.Any() is false) return;

        Dimensionality = _addends[0].Dimensionality;
        if (_addends.Any(a => a.Dimensionality != Dimensionality))
            throw new IncompatibleDimensionsException("SumExpression addends must all have same dimensionaltiy");
    }


    public Dimensionality Dimensionality { get; private set; }
    public IReadOnlyList<IExpression> Addends => _addends;
    public bool IsFullyDescribed => _addends.All(a => a.IsFullyDescribed);

    public Measurand? Value => IsFullyDescribed && _addends.Count > 0
        ? _addends.Select(a => a.Value!).Skip(1)
            .Aggregate(_addends[0].Value!, (acc, a) => acc.Plus(a, ErrorPropagation))
        : null;

    public void AddAddend(IExpression expression)
    {
        if (Addends.Any())
        {
            if (expression.Dimensionality != Dimensionality)
                throw new IncompatibleDimensionsException("Addends must match dimensionality of SumExpression");
        }
        else
        {
            Dimensionality = expression.Dimensionality;
        }
        _addends.Add(expression);
    }

    public bool RemoveAddend(IExpression expression)
    {
        return _addends.Remove(expression);
    }

    public override string ToString()
    {
        return $"({string.Join('+', Addends.Select(a => a.ToString()))})";
    }

    public int DegreesOfFreedom()
    {
        return _addends.Sum(a => a.DegreesOfFreedom());
    }

    /// <inheritdoc/>
    public NaryExpressionState GetState() =>
        new(NaryExpressionKind.Sum, Id, Addends.Select(a => a.Id).ToList(), ErrorPropagation);

    /// <inheritdoc/>
    public static SumExpression FromState(NaryExpressionState state, INodeResolver resolve) =>
        new(state.InnerIds.Select(resolve.Resolve<IExpression>))
        {
            Id = state.Id,
            ErrorPropagation = state.ErrorPropagation,
        };
}
