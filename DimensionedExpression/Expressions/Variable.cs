using Calcusystem.Core.Identity;
using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Exceptions;
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
/// dimensionality throws <see cref="IncompatibleDimensionsException"/>.
/// <br/><br/>
/// Optionally carries an <see cref="IProvenance"/> recording where its value came from; purely descriptive, it
/// never affects evaluation.
/// </summary>
public class Variable : ExpressionBase, IDirectExpression, ISnapshotting<Variable, VariableSnapshot>
{
    // ReSharper disable once InconsistentNaming
    protected Measurand? _value;
    /// <summary>A variable with no value yet.</summary>
    /// <exception cref="Exceptions.InvalidNameException"><paramref name="name"/> is not a valid name.</exception>
    public Variable(
        string name,
        Dimensionality dimensionality,
        string id = IdBase.CREATE_NEW_ID)
        : base(id)
    {
        Dimensionality = dimensionality;
        base.Name = name ?? throw new InvalidNameException("A variable needs a name.");
    }

    /// <summary>A variable with a value, whose dimensionality it takes.</summary>
    /// <exception cref="Exceptions.InvalidNameException"><paramref name="name"/> is not a valid name.</exception>
    public Variable(
        string name,
        Measurand measurand,
        string id = IdBase.CREATE_NEW_ID)
        : base(id)
    {
        Dimensionality = measurand.Dimensionality;
        _value = measurand;
        base.Name = name ?? throw new InvalidNameException("A variable needs a name.");
    }

    public override bool IsDirectlyMutable => true;
    public override bool IsFullyDescribed => Value != null;
    public override Dimensionality Dimensionality { get; }

    /// <summary><inheritdoc/></summary>
    /// <remarks>A leaf: a variable is computed from nothing, so it has no children.</remarks>
    public override IEnumerable<IExpression> Children => [];

    /// <summary><inheritdoc/></summary>
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

    /// <summary>What the model calls this variable. Never null, and fixed once the variable is built.</summary>
    public new string Name => base.Name!;

    /// <summary>
    /// Optional audit annotation describing where this variable's value came from. Null means provenance is not
    /// tracked. Created via <c>ProvenanceFactory</c>; purely descriptive — it does not affect evaluation.
    /// </summary>
    public IProvenance? Provenance { get; set; }

    public override string ToString()
    {
        return Name;
    }

    /// <inheritdoc/>
    public VariableSnapshot GetSnapshot() => new(
        Id,
        Name,
        Dimensionality.GetSnapshot(),
        _value?.GetSnapshot(),
        Provenance?.GetSnapshot());

    /// <summary><inheritdoc/></summary>
    /// <remarks>
    /// An unset variable keeps its declared dimensionality; a bound one takes its dimensionality from the
    /// measurand, which the constructor requires to agree with it anyway.
    /// </remarks>
    public static Variable FromSnapshot(VariableSnapshot snapshot)
    {
        var variable = snapshot.Value is { } value
            ? new Variable(snapshot.Name, Measurand.FromSnapshot(value), snapshot.Id)
            : new Variable(snapshot.Name, Dimensionality.FromSnapshot(snapshot.Dimensionality), snapshot.Id);

        if (snapshot.Provenance is { } provenance)
        {
            variable.Provenance = ProvenanceFactory.FromSnapshot(provenance);
        }

        return variable;
    }
}
