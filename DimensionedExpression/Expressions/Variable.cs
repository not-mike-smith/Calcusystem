using Calcusystem.Core.Identity;
using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Exceptions;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// A mutable leaf expression — a named quantity whose <see cref="Value"/> is set directly. Construct it unset
/// (dimensionality only) or with an initial <see cref="Measurand"/>; assigning a value of the wrong
/// dimensionality throws <see cref="IncompatibleDimensionsException"/>. <see cref="DegreesOfFreedom"/> is 0 once
/// valued, else 1.
/// <br/>
/// Optionally carries an <see cref="IProvenance"/> recording where its value came from; purely descriptive, it
/// never affects evaluation.
/// </summary>
public class Variable : ExpressionBase, IDirectExpression, ISnapshotting<Variable, VariableSnapshot>
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

    public override bool IsDirectlyMutable => true;
    public override bool IsFullyDescribed => Value != null;
    public override Dimensionality Dimensionality { get; }

    /// <inheritdoc/>
    /// <remarks>A leaf: a variable is computed from nothing, so it has no children.</remarks>
    public override IEnumerable<IExpression> Children => [];

    /// <inheritdoc/>
    /// <remarks>
    /// A leaf has nothing to combine, so it answers with its own entry if one was supplied and its stored value
    /// otherwise. That is the whole of the override mechanism: a caller seeds a trial value for this variable
    /// and every node above it computes normally, with no special case anywhere in the walk.
    /// </remarks>
    public override Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IUncertaintyPropagator? propagator = null) =>
        known.TryGetValue(this, out var supplied) ? supplied : _value;


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
    public VariableSnapshot GetSnapshot() => new(
        Id,
        Symbol,
        Dimensionality.GetSnapshot(),
        _value?.GetSnapshot(),
        Provenance?.GetSnapshot());

    /// <inheritdoc/>
    /// <remarks>
    /// An unset variable keeps its declared dimensionality; a bound one takes its dimensionality from the
    /// measurand, which the constructor requires to agree with it anyway.
    /// </remarks>
    public static Variable FromSnapshot(VariableSnapshot state)
    {
        var variable = state.Value is { } value
            ? new Variable(state.Symbol, Measurand.FromSnapshot(value), state.Id)
            : new Variable(state.Symbol, Dimensionality.FromSnapshot(state.Dimensionality), state.Id);

        if (state.Provenance is { } provenance)
        {
            variable.Provenance = ProvenanceFactory.FromSnapshot(provenance);
        }

        return variable;
    }
}
