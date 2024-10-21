using DimensionedExpression.Interfaces;

namespace DimensionedExpression.BaseModels;

public abstract class BinaryOperatorBase : IBinaryOperator
{
    public required string Id { get; init; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public required IExpression Lhs { get; set; }
    public required IExpression Rhs { get; set; }
    public abstract bool IsCommutative { get; }
    public abstract bool? IsSatisfied(); // TODO? move to extension?
    public abstract string Symbol { get; }

    public bool AreBothSidesFullyDescribed => Lhs.IsFullyDescribed && Rhs.IsFullyDescribed;
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
