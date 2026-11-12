using Calcusystem.Core.Identity;
using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Names;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Exceptions;

namespace Calcusystem.DimensionedExpression.Systems;

public class ExpressionSystem : IdBase, ISnapshottingNode<ExpressionSystem, ExpressionSystemSnapshot>
{
    public ExpressionSystem(string id) : base(id) { }

    /// <summary>
    /// Creates a new <see cref="ExpressionSystem"/> with an auto-generated ID.
    /// </summary>
    public static ExpressionSystem Create(string name, string description = "")
    {
        return new ExpressionSystem(IdBase.CREATE_NEW_ID)
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

    // Expressions and relationships together: names share one namespace.
    private readonly Dictionary<string, IIdentified> _named = new(StringComparer.Ordinal);

    /// <summary>Every <see cref="Variable"/> this system contains — the leaves values are supplied to.</summary>
    /// <remarks>
    /// Not merely the ones handed to <see cref="Add(IExpression)"/> directly. A variable reached through a
    /// derived expression or through a relationship's operand is just as much a part of this system, and saying
    /// so is what stops membership and reachability being two answers to one question — the same reasoning that
    /// made <see cref="Equations"/>, <see cref="CoherenceChecks"/> and <see cref="Requirements"/> views.
    /// </remarks>
    public IReadOnlyList<Variable> Variables => _variables;

    /// <summary>Every computed node this system contains, including nodes nested inside others.</summary>
    public IReadOnlyList<IExpression> DerivedExpressions => _derivedExpressions;

    /// <summary>
    /// Every relationship asserted over this system's expressions — definitions and constraints alike.
    /// </summary>
    /// <remarks>
    /// One list, because what a relationship does to the problem belongs to the operator, not to where it was
    /// filed. <see cref="IBinaryOperator.SolvingRole"/> carries it and
    /// <see cref="IBinaryOperator.IsDetermining"/> is what degrees-of-freedom code reads;
    /// keeping parallel lists would make membership a second, silently divergent answer to the same
    /// question. <see cref="Equations"/>, <see cref="CoherenceChecks"/> and <see cref="Requirements"/> are views.
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
    /// <exception cref="DuplicateNameException">
    /// A node being added has a name that another node, in the system or among those being added, already has.
    /// Nothing is added.
    /// </exception>
    public void Add(IExpression expression) => Absorb(NotYetHeld([expression]), relationship: null);

    /// <summary>Adds <paramref name="relationship"/>, and both of its operands with everything beneath them.</summary>
    /// <remarks>
    /// The fail-fast gate for authoring. Comparing quantities that share no scale is a modelling mistake, not a
    /// verdict — <c>10 kg ⌜&lt;⌟ 20 m</c> is not false, it is meaningless — and until now nothing said so,
    /// though <c>Quantity</c> has always refused to add a mass to a length. Evaluation stays defensive anyway:
    /// <c>MeasurandComparer</c> answers <c>Incomparable</c>, so a document assembled elsewhere still yields an
    /// undetermined verdict rather than a confident wrong one.
    /// </remarks>
    /// <exception cref="IncompatibleDimensionsException">The two operands carry different dimensions.</exception>
    /// <exception cref="DuplicateNameException">
    /// The relationship, or a node beneath it, has a name that another node already has. Nothing is added.
    /// </exception>
    public void Add(IBinaryOperator relationship)
    {
        // Dimensionality is known for every expression, bound or not, so this needs no values and holds for a
        // relationship over unknowns.
        if (relationship.Lhs.Dimensionality != relationship.Rhs.Dimensionality)
        {
            throw new IncompatibleDimensionsException(
                $"Relationship '{relationship.Id}' compares {relationship.Lhs.Dimensionality} with " +
                $"{relationship.Rhs.Dimensionality}.");
        }

        Absorb(NotYetHeld([relationship.Lhs, relationship.Rhs]), relationship);
    }

    /// <summary>The expression named <paramref name="name"/>, or null if the system has none.</summary>
    /// <remarks>
    /// Compared after normalization, and case-sensitively. A relationship's name finds nothing here, though no
    /// expression can share it; see <see cref="FindRelationship"/>.
    /// </remarks>
    public IExpression? Find(string name) => Named(name) as IExpression;

    /// <summary>The relationship named <paramref name="name"/>, or null if the system has none.</summary>
    /// <remarks>Compared as <see cref="Find"/> compares.</remarks>
    public IBinaryOperator? FindRelationship(string name) => Named(name) as IBinaryOperator;

    private IIdentified? Named(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _named.GetValueOrDefault(NodeName.Normalized(name));
    }

    /// <summary>The nodes beneath <paramref name="roots"/> that this system does not hold yet, each once.</summary>
    private List<IExpression> NotYetHeld(IEnumerable<IExpression> roots)
    {
        var incoming = new List<IExpression>();
        var seen = new HashSet<IExpression>();

        foreach (var node in roots.SelectMany(root => root.SelfAndDescendants()))
        {
            if (! _absorbed.Contains(node) && seen.Add(node)) incoming.Add(node);
        }

        return incoming;
    }

    // Every name is checked before anything is taken in, so a refusal leaves the system as it was.
    private void Absorb(List<IExpression> incoming, IBinaryOperator? relationship)
    {
        var claimed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in incoming)
        {
            if (node.Name is not { } name) continue;
            if (_named.ContainsKey(name) || ! claimed.Add(name)) throw new DuplicateNameException(name);
        }

        if (relationship?.Name is { } relationshipName)
        {
            // Adding a relationship the system already holds is not a second claim on its name.
            var heldByItself = ReferenceEquals(_named.GetValueOrDefault(relationshipName), relationship);

            if (! heldByItself && (_named.ContainsKey(relationshipName) || ! claimed.Add(relationshipName)))
            {
                throw new DuplicateNameException(relationshipName);
            }
        }

        foreach (var node in incoming)
        {
            _absorbed.Add(node);

            if (node is Variable variable) _variables.Add(variable);
            else _derivedExpressions.Add(node);

            if (node.Name is { } name) _named[name] = node;
        }

        if (relationship is null) return;

        _relationships.Add(relationship);
        if (relationship.Name is { } added) _named[added] = relationship;
    }

    /// <summary>Relationships that define a quantity — <see cref="SolvingRole.Equation"/>.</summary>
    public IEnumerable<IBinaryOperator> Equations =>
        Relationships.Where(r => r.SolvingRole is SolvingRole.Equation);

    /// <summary>
    /// Relationships asserting that separately computed routes to one quantity agree —
    /// <see cref="SolvingRole.Coherence"/>.
    /// </summary>
    public IEnumerable<IBinaryOperator> CoherenceChecks =>
        Relationships.Where(r => r.SolvingRole is SolvingRole.Coherence);

    /// <summary>Relationships that bound a value without producing one — <see cref="SolvingRole.Requirement"/>.</summary>
    public IEnumerable<IBinaryOperator> Requirements =>
        Relationships.Where(r => r.SolvingRole is SolvingRole.Requirement);

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
    public ExpressionSystemSnapshot GetSnapshot() => new(
        Id,
        Name,
        Description,
        _variables.Select(x => x.Id).ToList(),
        _derivedExpressions.Select(x => x.Id).ToList(),
        _relationships.Select(x => x.Id).ToList());

    /// <summary><inheritdoc/></summary>
    /// <remarks>
    /// The system resolves two different node types — expressions in two of its lists, operators in the third.
    /// That is why resolution is a per-reference query rather than one typed delegate.
    /// </remarks>
    public static ExpressionSystem FromSnapshot(ExpressionSystemSnapshot snapshot, INodeResolver resolve)
    {
        var system = new ExpressionSystem(snapshot.Id)
        {
            Name = snapshot.Name,
            Description = snapshot.Description,
        };

        foreach (var id in snapshot.VariableIds) system.Add(resolve.Resolve<Variable>(id));
        foreach (var id in snapshot.DerivedExpressionIds) system.Add(resolve.Resolve<IExpression>(id));
        foreach (var id in snapshot.RelationshipIds) system.Add(resolve.Resolve<IBinaryOperator>(id));
        return system;
    }
}
