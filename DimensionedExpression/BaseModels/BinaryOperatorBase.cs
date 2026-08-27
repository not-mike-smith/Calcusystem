using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Interfaces;

namespace Calcusystem.DimensionedExpression.BaseModels;

public abstract class BinaryOperatorBase : IBinaryOperator
{
    public required string Id { get; init; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public required IExpression Lhs { get; init; }
    public required IExpression Rhs { get; init; }
    public IProvenance? Provenance { get; set; }
    public abstract bool IsCommutative { get; }

    /// <inheritdoc/>
    public abstract string Symbol { get; }

    /// <inheritdoc/>
    public abstract bool IsSatisfiedGiven(Measurand lhs, Measurand rhs);

    /// <inheritdoc/>
    /// <remarks>
    /// Implemented once here rather than on each operator: resolving both sides and answering null if either is
    /// missing is identical for all thirteen, and only the comparison below the guard differs. That comparison is
    /// <see cref="IsSatisfiedGiven"/>, which is also what a calculation calls directly with values it has
    /// already computed.
    /// </remarks>
    public bool? IsSatisfied(
        IReadOnlyDictionary<Variable, Measurand>? overrides = null,
        IErrorPropagator? propagator = null)
    {
        // One walk per side. `ComputeIfDetermined` is not free, and a null answer is exactly the
        // "not fully described" case the guard used to ask for separately.
        var lhs = Lhs.ComputeIfDetermined(overrides, propagator);
        var rhs = Rhs.ComputeIfDetermined(overrides, propagator);
        if (lhs is null || rhs is null) return null;

        return IsSatisfiedGiven(lhs, rhs);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Virtual rather than abstract, and a requirement by default, because anything else is the exception: only
    /// the equality family can derive a value. An operator that overrides this takes the role through its own
    /// constructor, so the twelve that do not override it have no way to be constructed claiming otherwise.
    /// </remarks>
    public virtual SolvingRole SolvingRole => SolvingRole.Requirement;

    /// <inheritdoc/>
    public bool IsDetermining => SolvingRole is SolvingRole.Equation or SolvingRole.Coherence;

    /// <inheritdoc/>
    public IExpression? Subject => SolvingRole is SolvingRole.Requirement ? Lhs : null;

    /// <inheritdoc/>
    public IExpression? Criterion => SolvingRole is SolvingRole.Requirement ? Rhs : null;

    /// <summary>Which operator this is, for state capture. Declared alongside <see cref="Symbol"/>.</summary>
    protected abstract BinaryOperatorKind Kind { get; }

    /// <summary>
    /// Returns this operator's complete stored state. Every operator has the same shape — two operand
    /// references plus annotations — so this is implemented once here rather than thirteen times.
    /// </summary>
    public BinaryOperatorState GetState() =>
        new(Kind, Id, Lhs.Id, Rhs.Id, SolvingRole, Name, Description, Provenance?.GetState());

    public bool AreBothSidesFullyDescribed => Lhs.IsFullyDescribed && Rhs.IsFullyDescribed;

    /// <inheritdoc/>
    public IEnumerable<Variable> FreeVariables() =>
        Lhs.FreeVariables().Concat(Rhs.FreeVariables()).Distinct();
    public override string ToString()
    {
        return $"{Lhs} {Symbol} {Rhs}";
    }
}

public abstract class CommutativeOperatorBase : BinaryOperatorBase
{
    public override bool IsCommutative => true;
}

public abstract class NonCommutativeOperatorBase : BinaryOperatorBase
{
    public override bool IsCommutative => false;
}
