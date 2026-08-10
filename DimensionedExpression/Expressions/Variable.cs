using DimensionedExpression.Interfaces;
using Measurement.State;
using DimensionedExpression.State;
using DimensionedExpression.Provenance;
using Calcusystem.Core;
using Measurement;
using Measurement.Exceptions;

namespace DimensionedExpression.Expressions;

/// <summary>
/// A mutable leaf expression — a named quantity whose <see cref="Value"/> is set directly. Construct it unbound
/// (dimensionality only) or with an initial <see cref="Measurand"/>; assigning a value of the wrong
/// dimensionality throws <see cref="IncompatibleDimensionsException"/>. <see cref="DegreesOfFreedom"/> is 0 once
/// valued, else 1.
/// <br/>
/// Optionally carries an <see cref="IProvenance"/> recording where its value came from; purely descriptive, it
/// never affects evaluation.
/// </summary>
public class Variable : IdBase, IDirectExpression, IStateful<Variable, VariableState>
{
    // ReSharper disable once InconsistentNaming
    protected Measurand? _value;
    // ReSharper disable once InconsistentNaming
    protected string _symbol;

    public Variable(
        string symbol,
        Dimensionality dimensionality,
        string id = Constants.CREATE_NEW_ID)
        : base(id)
    {
        
        Dimensionality = dimensionality;
        _symbol = symbol;
    }

    public Variable(
        string symbol,
        Measurand measurand,
        string id = Constants.CREATE_NEW_ID)
        : base(id)
    {
        Dimensionality = measurand.Dimensionality;
        _value = measurand;
        _symbol = symbol;
    }

    public bool IsDirectlyMutable => true;
    public bool IsFullyDescribed => Value != null;
    public Dimensionality Dimensionality { get; }

    /// <inheritdoc/>
    /// <remarks>A leaf: a variable is computed from nothing, so it has no children.</remarks>
    public IEnumerable<IExpression> Children => [];

    public Measurand? Value
    {
        get => _value;
        set
        {
            if (value != null && value.Dimensionality != Dimensionality)
                throw new IncompatibleDimensionsException("Measurand must match dimensionality of Expression");

            _value = value;
        }
    }

    public string Symbol
    {
        get => _symbol;
        set => _symbol = value;
    }

    /// <summary>
    /// Optional audit annotation describing where this variable's value came from. Null means provenance is not
    /// tracked. Created via <c>ProvenanceFactory</c>; purely descriptive — it does not affect evaluation.
    /// </summary>
    public IProvenance? Provenance { get; set; }

    public override string ToString()
    {
        return Symbol;
    }

    /// <inheritdoc/>
    public VariableState GetState() => new(
        Id,
        Symbol,
        Dimensionality.GetState(),
        _value?.GetState(),
        Provenance?.GetState());

    /// <inheritdoc/>
    /// <remarks>
    /// An unbound variable keeps its declared dimensionality; a bound one takes its dimensionality from the
    /// measurand, which the constructor requires to agree with it anyway.
    /// </remarks>
    public static Variable FromState(VariableState state)
    {
        var variable = state.Value is { } value
            ? new Variable(state.Symbol, Measurand.FromState(value), state.Id)
            : new Variable(state.Symbol, Dimensionality.FromState(state.Dimensionality), state.Id);

        if (state.Provenance is { } provenance)
        {
            variable.Provenance = ProvenanceFactory.FromState(provenance);
        }

        return variable;
    }
}
