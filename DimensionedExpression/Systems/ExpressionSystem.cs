using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Core;
using Calcusystem.DimensionedExpression.Interfaces;

namespace Calcusystem.DimensionedExpression.Systems;

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

    /// <summary>
    /// Every relationship asserted over this system's expressions — definitions and constraints alike. The one
    /// list callers add to.
    /// </summary>
    /// <remarks>
    /// Definitions and constraints are one list because the distinction belongs to the operator, not to where it
    /// was filed. <see cref="IBinaryOperator.IsDetermining"/> is what the degrees-of-freedom calculation reads;
    /// keeping a parallel pair of lists would make membership a second, silently divergent answer to the same
    /// question. <see cref="Definitions"/> and <see cref="Constraints"/> remain as views over this list.
    /// </remarks>
    public List<IBinaryOperator> Relationships { get; } = new();

    /// <summary>
    /// The relationships that determine values — the equations counted against the unknowns when computing
    /// degrees of freedom. A view over <see cref="Relationships"/>; add through that.
    /// </summary>
    public IEnumerable<IBinaryOperator> Definitions => Relationships.Where(r => r.IsDetermining);

    /// <summary>
    /// The relationships that only check values — every relationship that is not a definition. A view over
    /// <see cref="Relationships"/>; add through that.
    /// </summary>
    public IEnumerable<IBinaryOperator> Constraints => Relationships.Where(r => ! r.IsDetermining);

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
        Relationships.Select(x => x.Id).ToList());

    /// <inheritdoc/>
    /// <remarks>
    /// The system resolves two different node types — expressions in two of its lists, operators in the third.
    /// That is why resolution is a per-reference query rather than one typed delegate.
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
        system.Relationships.AddRange(state.RelationshipIds.Select(resolve.Resolve<IBinaryOperator>));
        return system;
    }
}
