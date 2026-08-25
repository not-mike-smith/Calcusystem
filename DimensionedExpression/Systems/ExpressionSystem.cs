using Calcusystem.DimensionedExpression.BaseModels;
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

    private readonly List<Variable> _variables = new();
    private readonly List<IExpression> _derivedExpressions = new();
    private readonly List<IBinaryOperator> _relationships = new();
    private readonly HashSet<IExpression> _absorbed = new();

    /// <summary>Every <see cref="Variable"/> this system contains — the leaves values are supplied to.</summary>
    /// <remarks>
    /// Not merely the ones handed to <see cref="Add(IExpression)"/> directly. A variable reached through a
    /// derived expression or through a relationship's operand is just as much a part of this system, and saying
    /// so is what stops membership and reachability being two answers to one question — the same reasoning that
    /// made <see cref="Definitions"/> and <see cref="Constraints"/> views rather than lists.
    /// </remarks>
    public IReadOnlyList<Variable> Variables => _variables;

    /// <summary>Every computed node this system contains, including nodes nested inside others.</summary>
    public IReadOnlyList<IExpression> DerivedExpressions => _derivedExpressions;

    /// <summary>
    /// Every relationship asserted over this system's expressions — definitions and constraints alike.
    /// </summary>
    /// <remarks>
    /// Definitions and constraints are one list because the distinction belongs to the operator, not to where it
    /// was filed. <see cref="IBinaryOperator.IsDetermining"/> is what the degrees-of-freedom calculation reads;
    /// keeping a parallel pair of lists would make membership a second, silently divergent answer to the same
    /// question. <see cref="Definitions"/> and <see cref="Constraints"/> remain as views over this list.
    /// </remarks>
    public IReadOnlyList<IBinaryOperator> Relationships => _relationships;

    /// <summary>
    /// Adds <paramref name="expression"/> and everything beneath it.
    /// </summary>
    /// <remarks>
    /// Absorbing the whole subgraph is what makes the collections above truthful. A composite assembled from
    /// nodes the caller never mentioned separately still puts those nodes in this system — they are computed by
    /// <c>Calculate</c>, they are written by persistence, and a report that omitted them would be describing a
    /// different model than the one being evaluated. Absorbing eagerly is safe because an expression's operands
    /// are fixed at construction, so what is captured here cannot later drift from what the graph holds.
    /// </remarks>
    public void Add(IExpression expression) => Absorb(expression);

    /// <summary>Adds <paramref name="relationship"/>, and both of its operands with everything beneath them.</summary>
    public void Add(IBinaryOperator relationship)
    {
        Absorb(relationship.Lhs);
        Absorb(relationship.Rhs);
        _relationships.Add(relationship);
    }

    private void Absorb(IExpression expression)
    {
        foreach (var node in expression.SelfAndDescendants())
        {
            if (! _absorbed.Add(node)) continue;

            if (node is Variable variable) _variables.Add(variable);
            else _derivedExpressions.Add(node);
        }
    }

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

    /// <summary>Every expression this system contains: its variables and its computed nodes.</summary>
    /// <remarks>
    /// Complete, because <see cref="Add(IExpression)"/> absorbs whole subgraphs. There is no wider set to ask
    /// for — what the system lists and what it reaches are the same thing by construction.
    /// </remarks>
    public IEnumerable<IExpression> GetAllExpressions() => _variables.Concat<IExpression>(_derivedExpressions);

    /// <summary>
    /// Every node this system reaches, each once, children before parents — the order values can be computed in
    /// without ever needing one that has not been produced yet.
    /// </summary>
    /// <remarks>
    /// A question about the system's own structure, like <see cref="GetAllExpressions"/>, and the only form of
    /// the walk that ranges over several roots at once. Ordering is structural rather than orchestration: it
    /// says what depends on what, and decides nothing about whether or when anything is computed.
    /// </remarks>
    /// <exception cref="Exceptions.CyclicExpressionGraphException">The system's graph contains a cycle.</exception>
    public IReadOnlyList<IExpression> InDependencyOrder() =>
        ExpressionGraph.InDependencyOrder(GetAllExpressions());

    /// <inheritdoc/>
    public ExpressionSystemState GetState() => new(
        Id,
        Name,
        Description,
        _variables.Select(x => x.Id).ToList(),
        _derivedExpressions.Select(x => x.Id).ToList(),
        _relationships.Select(x => x.Id).ToList());

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

        foreach (var id in state.VariableIds) system.Add(resolve.Resolve<Variable>(id));
        foreach (var id in state.DerivedExpressionIds) system.Add(resolve.Resolve<IExpression>(id));
        foreach (var id in state.RelationshipIds) system.Add(resolve.Resolve<IBinaryOperator>(id));
        return system;
    }
}
