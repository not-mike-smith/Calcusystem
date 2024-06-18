namespace DimensionedExpression.Interfaces;

public interface IBinaryOperator
{
    public string Id { get; }
    IExpression Lhs { get; set; }
    IExpression Rhs { get; set; }
    bool IsCommutative { get; }
    bool? IsSatisfied();
    bool AreBothSidesFullyDescribed { get; }
}
