using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Interfaces;

namespace Calcusystem.DimensionedExpression.Traversal;

/// <summary>
/// Walks over an expression graph, written once against <see cref="IExpression.Children"/> rather than once per
/// node type.
/// </summary>
/// <remarks>
/// <para>
/// Every walk here deduplicates, because an expression graph is a <b>DAG, not a tree</b> — the same
/// sub-expression may be referenced from several parents, which is the whole reason nodes refer to neighbours by
/// id. Counting it once per reference is wrong for anything that asks "how many distinct things are there",
/// which is exactly what degrees of freedom asks.
/// </para>
/// <para>
/// Traversal is iterative rather than recursive: nothing forbids a deep graph, and a stack frame per node is an
/// avoidable failure mode.
/// </para>
/// </remarks>
public static class ExpressionTraversal
{
    /// <summary>
    /// <paramref name="root"/> and every node reachable from it, each yielded exactly once however many parents
    /// reference it. Order is unspecified beyond that.
    /// </summary>
    public static IEnumerable<IExpression> SelfAndDescendants(this IExpression root)
    {
        var seen = new HashSet<IExpression>();
        var pending = new Stack<IExpression>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var node = pending.Pop();

            // A repeat visit is a shared sub-expression, not a cycle: the graph is acyclic by construction,
            // since a node can only be given children that already exist.
            if (!seen.Add(node)) continue;

            yield return node;

            foreach (var child in node.Children)
            {
                pending.Push(child);
            }
        }
    }

    /// <summary>
    /// Computes <paramref name="root"/>'s value with propagated uncertainty, or returns <see langword="null"/>
    /// if any leaf it depends on is still unbound.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Named for what it costs.</b> This walks the whole graph beneath the node on every call and caches
    /// nothing, so a sub-expression shared by three parents is computed three times. It is a method, not a
    /// property, because a property invites callers to treat it as field access and call it in a loop.
    /// </para>
    /// <para>
    /// Nothing is memoised on purpose: a node has no way to learn that a leaf beneath it was reassigned, so a
    /// cached answer would go stale silently. Caching belongs to a caller that knows over what scope the graph
    /// is unchanged — <c>Calcusystem.Analysis</c>'s <c>Calculate</c> computes each node once per run. Prefer it
    /// for anything beyond a one-off read.
    /// </para>
    /// <para>
    /// One implementation rather than one per node type: each node contributes <see cref="IExpression.Children"/>
    /// and <see cref="IExpression.ComputeFrom"/>, and this is the only way those two compose for a single node
    /// standing alone.
    /// </para>
    /// </remarks>
    public static Measurand? CalculateValueIfDetermined(
        this IExpression root,
        IErrorPropagator? propagator = null)
    {
        var known = new Dictionary<IExpression, Measurand>();

        foreach (var node in root.InDependencyOrder())
        {
            if (node.Children.All(known.ContainsKey) && node.ComputeFrom(known, propagator) is { } value)
            {
                known[node] = value;
            }
        }

        return known.TryGetValue(root, out var result) ? result : null;
    }

    /// <summary>
    /// <paramref name="root"/> and every node reachable from it, each once, children before parents — the order
    /// a value can be computed in without ever needing one that has not been produced yet.
    /// </summary>
    /// <exception cref="CyclicExpressionGraphException">The graph contains a cycle.</exception>
    public static IEnumerable<IExpression> InDependencyOrder(this IExpression root) =>
        InDependencyOrder([root]);

    /// <summary>
    /// Every node reachable from <paramref name="roots"/>, each once, children before parents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Iterative post-order rather than recursion, for the reason the other walks here are: nothing bounds how
    /// deep a graph can be, and a stack frame per node is an avoidable way to fail. The visited set makes a node
    /// shared by several parents appear once, positioned before the first of them.
    /// </para>
    /// <para>
    /// The visited set also stops a cycle from descending forever, but it does not make the answer meaningful —
    /// a cycle leaves some node emitted before an operand it depends on, and a caller folding over the order
    /// would silently find that operand absent. So the order is checked before it is handed out: every node must
    /// follow all of its own children.
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

            if (!seen.Add(node)) continue;

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

    /// <summary>
    /// The distinct unbound leaf variables reachable from <paramref name="root"/> — the values that must be
    /// supplied before it can produce a <c>Value</c>, and the unknowns it contributes to a system's degrees of
    /// freedom.
    /// </summary>
    /// <remarks>
    /// Only a <see cref="Variable"/> can be free: it is the sole node whose value is assigned rather than
    /// computed, so it is the only thing a solver could ever be asked to determine. A computed node with unbound
    /// leaves beneath it is not itself an unknown — it is the path by which those leaves are reached.
    /// </remarks>
    public static IEnumerable<Variable> FreeVariables(this IExpression root) =>
        root.SelfAndDescendants().OfType<Variable>().Where(v => v.IsFullyDescribed is false);

    /// <summary>
    /// The distinct unbound leaf variables reachable from either side of <paramref name="relationship"/> — the
    /// unknowns the relationship is incident on.
    /// </summary>
    public static IEnumerable<Variable> FreeVariables(this IBinaryOperator relationship) =>
        relationship.Lhs.FreeVariables()
            .Concat(relationship.Rhs.FreeVariables())
            .Distinct();
}
