namespace DimensionedExpression.Interfaces;

public interface IBinaryOperator
{
    public string Id { get; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    IExpression Lhs { get; set; }
    IExpression Rhs { get; set; }
    bool IsCommutative { get; }
    bool? IsSatisfied();
    bool AreBothSidesFullyDescribed { get; }
}
