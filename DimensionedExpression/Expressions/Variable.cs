using DimensionedExpression.Interfaces;
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
public class Variable : IdBase, IDirectExpression
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
    public int DegreesOfFreedom()
    {
        return IsFullyDescribed ? 0 : 1;
    }

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
}
