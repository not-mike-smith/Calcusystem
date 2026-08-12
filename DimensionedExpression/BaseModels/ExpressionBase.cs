using Calcusystem.Core;
using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Interfaces;

namespace Calcusystem.DimensionedExpression.BaseModels;

/// <summary>
/// Base for every expression node, supplying the walks that are derivable from
/// <see cref="IExpression.Children"/> and <see cref="IExpression.ComputeFrom"/>.
/// </summary>
/// <remarks>
/// <para>
/// A node type contributes two things — what its operands are, and how their values combine. Everything else a
/// node can be asked is a consequence of those two, has exactly one sensible implementation, and lives here so
/// that adding a node type does not mean rewriting any of it.
/// </para>
/// <para>
/// These are declared on <see cref="IExpression"/> and implemented here, rather than being extension methods, so
/// that they are part of the contract and are discoverable on the interface. A type implementing
/// <see cref="IExpression"/> without deriving from this class must supply them itself; deriving is the expected
/// path.
/// </para>
/// </remarks>
public abstract class ExpressionBase : IdBase, IExpression
{
    protected ExpressionBase(string id = Constants.CREATE_NEW_ID) : base(id) { }

    /// <inheritdoc/>
    public abstract bool IsDirectlyMutable { get; }

    /// <inheritdoc/>
    public abstract bool IsFullyDescribed { get; }

    /// <inheritdoc/>
    public abstract Dimensionality Dimensionality { get; }

    /// <inheritdoc/>
    public abstract IEnumerable<IExpression> Children { get; }

    /// <inheritdoc/>
    public abstract Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IErrorPropagator? propagator = null);

    /// <inheritdoc/>
    public Measurand? ComputeIfDetermined(
        IReadOnlyDictionary<Variable, Measurand>? overrides = null,
        IErrorPropagator? propagator = null)
    {
        var known = new Dictionary<IExpression, Measurand>();

        if (overrides is not null)
        {
            foreach (var (variable, value) in overrides) known[variable] = value;
        }

        foreach (var node in InDependencyOrder())
        {
            if (node.ComputeFrom(known, propagator) is { } value) known[node] = value;
        }

        return known.GetValueOrDefault(this);
    }

    /// <inheritdoc/>
    public IEnumerable<IExpression> SelfAndDescendants()
    {
        var seen = new HashSet<IExpression>();
        var pending = new Stack<IExpression>();
        pending.Push(this);

        while (pending.Count > 0)
        {
            var node = pending.Pop();

            // A repeat visit is a shared sub-expression, which is legitimate; a cycle is caught by
            // InDependencyOrder, which is the only walk whose answer a cycle would corrupt.
            if (! seen.Add(node)) continue;

            yield return node;

            foreach (var child in node.Children) pending.Push(child);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<Variable> FreeVariables() =>
        SelfAndDescendants().OfType<Variable>().Where(v => ! v.IsFullyDescribed);

    /// <inheritdoc/>
    public IReadOnlyList<IExpression> InDependencyOrder() => InDependencyOrder([this]);

    /// <summary>
    /// Every node reachable from <paramref name="roots"/>, each once, children before parents — the order values
    /// can be computed in without ever needing one that has not been produced yet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Static because a calculation orders a whole system at once, and the roots of a system are a collection
    /// rather than a node.
    /// </para>
    /// <para>
    /// Iterative rather than recursive: nothing bounds how deep a graph can be, and a stack frame per node is an
    /// avoidable way to fail. The visited set makes a node shared by several parents appear once, positioned
    /// before the first of them.
    /// </para>
    /// <para>
    /// That visited set also stops a cycle descending forever, but it does not make the answer meaningful — a
    /// cycle leaves some node ordered before an operand it depends on, and a caller folding over the order would
    /// find that operand absent and report a value as unresolvable when nothing is actually missing. So the
    /// order is checked before it is handed out: every node must follow all of its own children.
    /// </para>
    /// </remarks>
    /// <exception cref="CyclicExpressionGraphException">The graph contains a cycle.</exception>
    public static IReadOnlyList<IExpression> InDependencyOrder(IEnumerable<IExpression> roots)
    {
        var order = new List<IExpression>();
        var seen = new HashSet<IExpression>();
        var pending = new Stack<(IExpression Node, bool ChildrenExpanded)>();

        foreach (var root in roots) pending.Push((root, false));

        while (pending.Count > 0)
        {
            var (node, childrenExpanded) = pending.Pop();

            if (childrenExpanded)
            {
                order.Add(node);
                continue;
            }

            if (! seen.Add(node)) continue;

            pending.Push((node, true));
            foreach (var child in node.Children) pending.Push((child, false));
        }

        var position = new Dictionary<IExpression, int>(order.Count);
        for (var i = 0; i < order.Count; i++) position[order[i]] = i;

        foreach (var node in order)
        {
            foreach (var child in node.Children)
            {
                // `>=`, not `>`: a node that is its own operand shares its position with itself, and two
                // distinct nodes never share one, so equality catches the self-loop and nothing else.
                if (position[child] >= position[node]) throw new CyclicExpressionGraphException(node, child);
            }
        }

        return order;
    }
}
