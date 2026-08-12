using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Provenance;
using Calcusystem.DimensionedExpression.State;

namespace Calcusystem.DimensionedExpression.BaseModels;

public abstract class BinaryOperatorBase : IBinaryOperator
{
    public required string Id { get; init; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public required IExpression Lhs { get; set; }
    public required IExpression Rhs { get; set; }
    public IProvenance? Provenance { get; set; }
    public abstract bool IsCommutative { get; }
    public abstract bool? IsSatisfied(); // TODO? move to extension?
    public abstract string Symbol { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Virtual rather than abstract, and false by default, because determining is the exception: only the
    /// equality family can derive a value. An operator that overrides this takes the flag through its own
    /// constructor, so the twelve that do not override it have no way to be constructed claiming otherwise.
    /// </remarks>
    public virtual bool IsDetermining => false;

    /// <summary>Which operator this is, for state capture. Declared alongside <see cref="Symbol"/>.</summary>
    protected abstract BinaryOperatorKind Kind { get; }

    /// <summary>
    /// Returns this operator's complete stored state. Every operator has the same shape — two operand
    /// references plus annotations — so this is implemented once here rather than thirteen times.
    /// </summary>
    public BinaryOperatorState GetState() =>
        new(Kind, Id, Lhs.Id, Rhs.Id, IsDetermining, Name, Description, Provenance?.GetState());

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

    public void SwapSides()
    {
        (Lhs, Rhs) = (Rhs, Lhs);
    }
}

public abstract class NonCommutativeOperatorBase : BinaryOperatorBase
{
    public override bool IsCommutative => false;
}
