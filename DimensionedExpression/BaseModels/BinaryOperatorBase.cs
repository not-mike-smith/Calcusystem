using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Quantities;

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

    /// <summary>
    /// The comparisons this operator asserts, taken together. Every one must hold for the operator to be
    /// satisfied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What each operator <i>declares</i> in place of the interval arithmetic it used to write. All thirteen
    /// turned out to be conjunctions of landmark comparisons, so the conjunction is stated once here and the
    /// operators state only their own terms — which also makes the assertion readable without following it into
    /// an implementation.
    /// </para>
    /// <para>
    /// Exposed rather than private because it is the operator's own account of what it checks, and a report that
    /// wants to say <i>which</i> comparison failed needs the terms, not just the verdict.
    /// </para>
    /// </remarks>
    public abstract IReadOnlyList<ComparisonRule> Rules { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Implemented once over <see cref="Rules"/>. Kleene conjunction, so a rule that cannot be answered leaves
    /// the verdict unknown rather than failing it — see <see cref="ComparisonRule.AllSatisfied"/>.
    /// </remarks>
    public virtual bool? IsSatisfiedGiven(Measurand lhs, Measurand rhs) =>
        ComparisonRule.AllSatisfied(Rules, lhs, rhs);

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
    /// <remarks>
    /// Virtual for the one operator that carries state of its own: <see cref="EqualityOperator"/> adds its
    /// agreement rule on top of what is captured here. Overriding beats a hook on this class, which would put a
    /// member for equality's semantics on the twelve operators that have none.
    /// </remarks>
    public virtual BinaryOperatorState GetState() =>
        new(Kind, Id, Lhs.Id, Rhs.Id, SolvingRole, null, null, Name, Description, Provenance?.GetState());

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
