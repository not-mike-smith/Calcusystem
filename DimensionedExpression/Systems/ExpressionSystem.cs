using DimensionedExpression.BaseModels;
using DimensionedExpression.Expressions;
using DimensionedExpression.Interfaces;

namespace DimensionedExpression.Systems;

public class ExpressionSystem : IdBase
{
    public ExpressionSystem(string id) : base(id) { }

    /// <summary>
    /// Creates a new <see cref="ExpressionSystem"/> with an auto-generated ID.
    /// </summary>
    public static ExpressionSystem Create(string name, string description = "")
    {
        return new ExpressionSystem(Constants.CREATE_NEW)
        {
            Name = name,
            Description = description,
        };
    }

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<Variable> DirectExpressions { get; } = new();
    public List<IExpression> DerivedExpressions { get; } = new();
    public List<IBinaryOperator> Definitions { get; } = new();
    public List<IBinaryOperator> Constraints { get; } = new();

    public IEnumerable<IExpression> GetAllExpressions()
    {
        return DirectExpressions.Concat(DerivedExpressions);
    }
}
