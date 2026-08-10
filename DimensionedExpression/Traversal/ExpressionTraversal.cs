using DimensionedExpression.Expressions;
using DimensionedExpression.Interfaces;

namespace DimensionedExpression.Traversal;

/// <summary>
/// Walks over an expression graph, written once against <see cref="IExpression.Children"/> rather than once per
/// node type.
/// </summary>
/// <remarks>
/// <para>
/// Every walk here deduplicates by <c>Id</c>, because an expression graph is a <b>DAG, not a tree</b> — the same
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
        var seen = new HashSet<string>();
        var pending = new Stack<IExpression>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var node = pending.Pop();

            // A repeat visit is a shared sub-expression, not a cycle: the graph is acyclic by construction,
            // since a node can only be given children that already exist.
            if (!seen.Add(node.Id)) continue;

            yield return node;

            foreach (var child in node.Children)
            {
                pending.Push(child);
            }
        }
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
            .DistinctBy(v => v.Id);
}
