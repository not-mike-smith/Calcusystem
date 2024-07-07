using DimensionedExpression.BaseModels;
using DimensionedExpression.Interfaces;

namespace DimensionedExpression.Systems;

public class ExpressionSystem : IdBase
{
    public ExpressionSystem(string id) : base(id)
    {
        // TODO name creation in a factory?
    }

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<IExpression> DirectExpressions { get; } = new();
    public List<IExpression> DerivedExpressions { get; } = new();
    public List<IBinaryOperator> Definitions { get; } = new();
    public List<IBinaryOperator> Constraints { get; } = new();

    public IEnumerable<IExpression> GetAllExpressions()
    {
        return DirectExpressions.Concat(DerivedExpressions);
    }
}
