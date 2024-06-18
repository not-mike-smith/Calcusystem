namespace Calcusystem.Serialization.Dtos;

public class ExpressionSystem
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<SingleVariable> DirectExpressions { get; } = new();
    public List<ExpressionBase> DerivedExpressions { get; } = new();
    public List<BinaryOperator> Definitions { get; } = new();
    public List<BinaryOperator> Constraints { get; } = new();
}
