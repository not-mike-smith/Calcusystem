namespace Calcusystem.Serialization.Dtos;

public class ExpressionSystem
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public List<SingleVariable> DirectExpressions { get; } = new();
    public List<SingleDerivedVariable> SingleDerivedVariables { get; } = new();
    public List<ListDerivedVariable> ListDerivedVariables { get; } = new();
    public List<PairDerivedVariable> PairDerivedVariables { get; } = new();
    public List<BinaryOperator> Definitions { get; } = new();
    public List<BinaryOperator> Constraints { get; } = new();
}
