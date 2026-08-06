using DimensionedExpression.Expressions;
using DimensionedExpression.State;
using Calcusystem.Core;
using DimensionedExpression.Interfaces;

namespace DimensionedExpression.Systems;

public class ExpressionSystem : IdBase, IStatefulNode<ExpressionSystem, ExpressionSystemState>
{
    public ExpressionSystem(string id) : base(id) { }

    /// <summary>
    /// Creates a new <see cref="ExpressionSystem"/> with an auto-generated ID.
    /// </summary>
    public static ExpressionSystem Create(string name, string description = "")
    {
        return new ExpressionSystem(Constants.CREATE_NEW_ID)
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

    /// <inheritdoc/>
    public ExpressionSystemState GetState() => new(
        Id,
        Name,
        Description,
        DirectExpressions.Select(x => x.Id).ToList(),
        DerivedExpressions.Select(x => x.Id).ToList(),
        Definitions.Select(x => x.Id).ToList(),
        Constraints.Select(x => x.Id).ToList());

    /// <inheritdoc/>
    /// <remarks>
    /// The system resolves two different node types — expressions in two of its lists, operators in the other
    /// two. That is why resolution is a per-reference query rather than one typed delegate.
    /// </remarks>
    public static ExpressionSystem FromState(ExpressionSystemState state, INodeResolver resolve)
    {
        var system = new ExpressionSystem(state.Id)
        {
            Name = state.Name,
            Description = state.Description,
        };

        system.DirectExpressions.AddRange(state.DirectExpressionIds.Select(resolve.Resolve<Variable>));
        system.DerivedExpressions.AddRange(state.DerivedExpressionIds.Select(resolve.Resolve<IExpression>));
        system.Definitions.AddRange(state.DefinitionIds.Select(resolve.Resolve<IBinaryOperator>));
        system.Constraints.AddRange(state.ConstraintIds.Select(resolve.Resolve<IBinaryOperator>));
        return system;
    }
}
