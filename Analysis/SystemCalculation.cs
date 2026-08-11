using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.DimensionedExpression.Traversal;
using Calcusystem.Measurement;

namespace Calcusystem.Analysis;

/// <summary>
/// Calculating a system: working out everything its current values and relationships determine.
/// </summary>
/// <remarks>
/// An extension rather than a method on <see cref="ExpressionSystem"/> so that it reads as one
/// (<c>system.Calculate()</c>) without the expression layer having to know about this one. That layer assembles
/// and describes a graph and orchestrates nothing; keeping the dependency pointing this way is also what lets a
/// second strategy — a solver, an interval evaluator — sit beside this one rather than inside the domain type.
/// </remarks>
public static class SystemCalculation
{
    private static readonly IReadOnlyDictionary<Variable, Measurand> NoOverrides =
        new Dictionary<Variable, Measurand>();

    /// <summary>
    /// Calculates every expression in <paramref name="system"/>, resolving what it can and reporting what it
    /// could not.
    /// </summary>
    /// <param name="system">The system to calculate. Never mutated.</param>
    /// <param name="overrides">
    /// Values supplied for this calculation only, taking precedence over a variable's own. This is how a caller
    /// calculates at trial values without writing them into the model — see the assembly README.
    /// </param>
    /// <remarks>
    /// Each node is computed once: nodes are visited in dependency order and handed the values already
    /// established, so a sub-expression shared by three parents is computed once rather than three times as
    /// <c>CalculateValueIfDetermined()</c> would.
    /// </remarks>
    public static Calculation Calculate(
        this ExpressionSystem system,
        IReadOnlyDictionary<Variable, Measurand>? overrides = null)
    {
        overrides ??= NoOverrides;

        var listed = system.GetAllExpressions().ToList();

        // Seeded with the overrides, so a variable that has one finds itself already answered. Nothing here
        // needs to know a leaf from a composite — `ComputeFrom` is where that distinction lives.
        var values = new Dictionary<IExpression, Measurand>();
        foreach (var (variable, value) in overrides) values[variable] = value;

        foreach (var node in InDependencyOrder(listed))
        {
            // Children come first in this ordering, so a missing one means an unbound leaf somewhere beneath
            // and this node simply does not resolve.
            if (node.Children.Any(child => values.ContainsKey(child) is false)) continue;

            if (node.ComputeFrom(values) is { } value) values[node] = value;
        }

        var unresolved = listed.Where(e => values.ContainsKey(e) is false).ToList();

        var missing = listed
            .SelectMany(e => e.FreeVariables())
            .Where(v => overrides.ContainsKey(v) is false)
            .Distinct()
            .ToList();

        return new Calculation(overrides, values, unresolved, missing);
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

            if (seen.Add(node) is false) continue;

            pending.Push((node, true));
            foreach (var child in node.Children) pending.Push((child, false));
        }

        return order;
    }
}
