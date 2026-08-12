using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Core;
using Calcusystem.DimensionedExpression.BaseModels;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Exceptions;
using Calcusystem.DimensionedExpression.Exceptions;

namespace Calcusystem.DimensionedExpression.Expressions;

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
        _dimensionality = dimensionality;
    }

    public SumExpression(IEnumerable<IExpression> addends)
    {
        _addends = addends.ToList();
        if (_addends.Any() is false) return;

        _dimensionality = _addends[0].Dimensionality;
        if (_addends.Any(a => a.Dimensionality != Dimensionality))
            throw new IncompatibleDimensionsException("SumExpression addends must all have same dimensionaltiy");
    }


    private Dimensionality _dimensionality;

    /// <inheritdoc/>
    public override Dimensionality Dimensionality => _dimensionality;
    public IReadOnlyList<IExpression> Addends => _addends;
    public override bool IsFullyDescribed => _addends.All(a => a.IsFullyDescribed);


    /// <inheritdoc/>
    /// <remarks>Addends are read in declaration order, so an addend listed twice contributes twice.</remarks>
    public override Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IErrorPropagator? propagator = null)
    {
        if (_addends.Count == 0 || _addends.Any(a => known.ContainsKey(a) is false)) return null;

        // One n-ary call rather than folding pairwise: the propagator combines all the errors at once instead
        // of building an intermediate Measurand per addend.
        return Measurand.Sum(ErrorPropagation, propagator, _addends.Select(a => known[a]));
    }


    public void AddAddend(IExpression expression)
    {
        if (Addends.Any())
        {
            if (expression.Dimensionality != Dimensionality)
                throw new IncompatibleDimensionsException("Addends must match dimensionality of SumExpression");
        }
        else
        {
            _dimensionality = expression.Dimensionality;
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

    /// <inheritdoc/>
    public override IEnumerable<IExpression> Children => _addends;

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
