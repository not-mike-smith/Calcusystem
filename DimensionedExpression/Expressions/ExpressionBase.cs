using Calcusystem.Core.Identity;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Expressions;

/// <summary>
/// Base for every expression node, supplying the walks that are derivable from
/// <see cref="IExpression.Children"/> and <see cref="IExpression.ComputeFrom"/>.
/// </summary>
/// <remarks>
/// A type implementing <see cref="IExpression"/> without deriving from this class must supply every walk
/// itself; deriving is the expected path.
/// </remarks>
public abstract class ExpressionBase : IdBase, IExpression
{
    protected ExpressionBase(string id = IdBase.CREATE_NEW_ID) : base(id) { }

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
        IUncertaintyPropagator? propagator = null);

    /// <inheritdoc/>
    public Measurand? ComputeIfFullyDescribed(
        IReadOnlyDictionary<Variable, Measurand>? overrides = null,
        IUncertaintyPropagator? propagator = null)
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
    public IEnumerable<Variable> UnsetVariables() =>
        SelfAndDescendants().OfType<Variable>().Where(v => ! v.IsFullyDescribed);

    /// <inheritdoc/>
    public IReadOnlyList<IExpression> InDependencyOrder() => ExpressionGraph.InDependencyOrder([this]);
}
