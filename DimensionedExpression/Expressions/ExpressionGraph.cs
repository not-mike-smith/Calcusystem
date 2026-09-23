using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Interfaces;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Walks that range over a <i>set</i> of nodes rather than one node.
/// </summary>
/// <remarks>
/// Internal, and deliberately not a member of anything. A node's own walks belong on <see cref="IExpression"/>,
/// where they are part of the contract — but ordering several roots at once is not something any single node can
/// be asked, so there is nothing to hang it on. Parking it as a static on <see cref="ExpressionBase"/> made
/// callers reference a base class they do not derive from, which is a fair sign the method was never about
/// inheritance. It is reached through <c>IExpression.InDependencyOrder()</c> and
/// <c>ExpressionSystem.InDependencyOrder()</c> instead.
/// </remarks>
internal static class ExpressionGraph
{
    /// <summary>
    /// Every node reachable from <paramref name="roots"/>, each once, children before parents — the order values
    /// can be computed in without ever needing one that has not been produced yet.
    /// </summary>
    /// <remarks>
    /// A node shared by several parents appears once, before the first of them. The order is verified before it
    /// is handed out — every node must follow all of its own children — because a visited set stops a cycle
    /// descending forever without making the resulting order meaningful.
    /// </remarks>
    /// <exception cref="CyclicExpressionGraphException">The graph contains a cycle.</exception>
    internal static IReadOnlyList<IExpression> InDependencyOrder(IEnumerable<IExpression> roots)
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
