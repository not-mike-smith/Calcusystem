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
    public IReadOnlyList<IExpression> InDependencyOrder() => ExpressionGraph.InDependencyOrder([this]);
}
