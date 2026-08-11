using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.DimensionedExpression.Traversal;
using Calcusystem.Measurement;

namespace Calcusystem.Analysis;

/// <summary>
/// Computes everything a system can currently produce.
/// </summary>
public static class SystemEvaluator
{
    /// <summary>
    /// Evaluates every expression in <paramref name="system"/>, resolving what it can and reporting what it
    /// could not.
    /// </summary>
    /// <param name="system">The system to evaluate. Never mutated.</param>
    /// <param name="bindings">
    /// Values supplied for this evaluation only, keyed by variable id, taking precedence over a variable's own
    /// value. This is how a caller computes at trial values without writing them into the model — see the
    /// assembly README.
    /// </param>
    /// <remarks>
    /// Unlike <see cref="IExpression.Value"/>, which re-walks to the leaves on every access, this computes each
    /// node once: nodes are visited in dependency order and each is handed operands already computed. A
    /// sub-expression shared by three parents is evaluated once, not three times.
    /// </remarks>
    public static EvaluationResult Evaluate(
        ExpressionSystem system,
        IReadOnlyDictionary<string, Measurand>? bindings = null)
    {
        var listed = system.GetAllExpressions().ToList();
        var values = new Dictionary<string, Measurand>();

        foreach (var node in InDependencyOrder(listed))
        {
            // Children come first in this ordering, so anything present is already resolved. A missing operand
            // means an unbound leaf somewhere beneath, and this node simply does not resolve.
            var operands = new List<Measurand>();
            var complete = true;

            foreach (var child in node.Children)
            {
                if (values.TryGetValue(child.Id, out var operand))
                {
                    operands.Add(operand);
                }
                else
                {
                    complete = false;
                    break;
                }
            }

            if (complete is false) continue;

            var value = node is Variable variable && bindings is not null
                        && bindings.TryGetValue(variable.Id, out var bound)
                ? bound
                : node.ComputeFrom(operands);

            if (value is not null) values[node.Id] = value;
        }

        var unresolved = listed.Where(e => values.ContainsKey(e.Id) is false).ToList();

        var missing = listed
            .SelectMany(e => e.FreeVariables())
            .Where(v => bindings is null || bindings.ContainsKey(v.Id) is false)
            .DistinctBy(v => v.Id)
            .ToList();

        return new EvaluationResult(values, unresolved, missing);
    }

    /// <summary>
    /// Every node reachable from <paramref name="roots"/>, each once, children before parents.
    /// </summary>
    /// <remarks>
    /// Iterative post-order rather than recursion, for the same reason the traversal helpers are: nothing bounds
    /// how deep a graph can be, and a stack frame per node is an avoidable way to fail. The visited set makes a
    /// node shared by several parents appear once, positioned before the first of them.
    /// </remarks>
    private static IEnumerable<IExpression> InDependencyOrder(IEnumerable<IExpression> roots)
    {
        var order = new List<IExpression>();
        var seen = new HashSet<string>();
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

            if (seen.Add(node.Id) is false) continue;

            pending.Push((node, true));
            foreach (var child in node.Children) pending.Push((child, false));
        }

        return order;
    }
}
