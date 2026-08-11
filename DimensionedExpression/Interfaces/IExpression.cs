using Calcusystem.Core;
using Calcusystem.Measurement;

namespace Calcusystem.DimensionedExpression.Interfaces;

/// <summary>
/// A node in a dimensioned expression tree — a leaf variable or a computed combination of other nodes.
/// A node's <see cref="Dimensionality"/> is always known (structural), but its <see cref="Value"/> is produced
/// only once every leaf it depends on has been given a value.
/// </summary>
/// <remarks>
/// Implementations compute <see cref="Value"/> lazily on each access from their current children — there is no
/// caching and no separate evaluate step. Arithmetic and uncertainty propagation are delegated to
/// <see cref="Measurand"/>; this layer only assembles and walks the tree.
/// </remarks>
public interface IExpression : IIdentified
{
    /// <summary>
    /// Whether this node's value can be set directly. True for leaf variables (<see cref="IDirectExpression"/>);
    /// false for computed nodes, whose value derives from their children.
    /// </summary>
    bool IsDirectlyMutable { get; }

    /// <summary>
    /// Whether every leaf this node depends on has a value, so <see cref="Value"/> is non-null. Equivalent to
    /// <c>DegreesOfFreedom() == 0</c>.
    /// </summary>
    bool IsFullyDescribed { get; }

    /// <summary>
    /// The physical dimension of this node, known structurally and always available — even before any values are
    /// supplied.
    /// </summary>
    Dimensionality Dimensionality { get; }

    /// <summary>
    /// Computes this node's value with propagated uncertainty, or returns <see langword="null"/> if any leaf it
    /// depends on is still unbound.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A method, and named for what it costs.</b> This walks the whole graph beneath the node on every call
    /// and caches nothing, so a sub-expression shared by three parents is computed three times. A property would
    /// invite callers to treat it as field access and to call it in a loop.
    /// </para>
    /// <para>
    /// Nothing is memoised here on purpose: a node has no way to learn that a leaf beneath it was reassigned, so
    /// a cached answer here could silently go stale. Caching belongs to a caller that knows the scope over which
    /// the graph is unchanged — see <c>SystemEvaluator</c>, which computes each node once per run by walking in
    /// dependency order and feeding results to <see cref="ComputeFrom"/>. Prefer it for anything beyond a
    /// one-off read.
    /// </para>
    /// </remarks>
    Measurand? CalculateValueIfDetermined();

    /// <summary>
    /// The nodes this one is computed from, in operand order; empty for a leaf. The single accessor every graph
    /// walk goes through — free-variable collection, dependency ordering, and incidence are all one traversal
    /// over this rather than a switch over node types.
    /// </summary>
    /// <remarks>
    /// A node may appear as a child of more than one parent: the graph is a DAG, not a tree, and shared
    /// sub-expressions are the point of referencing neighbours by id. Any walk must therefore deduplicate by
    /// <see cref="IIdentified.Id"/> — see <c>ExpressionTraversal</c>, which does.
    /// </remarks>
    IEnumerable<IExpression> Children { get; }

    /// <summary>
    /// This node's value given its operands' values, supplied in <see cref="Children"/> order — the node's own
    /// arithmetic and uncertainty propagation, with the walk that produced the operands factored out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Value"/> is this applied to children that computed themselves recursively; an evaluator is the
    /// same function applied to operands it computed in dependency order, remembering each result. That is the
    /// point of the split: a node owns how it combines values, and a caller owns the order values are produced
    /// in and whether any of them are worth keeping. Neither can be memoised or overridden through
    /// <see cref="CalculateValueIfDetermined"/> alone, because it reaches all the way to the leaves on every call.
    /// </para>
    /// <para>
    /// A leaf has no operands and answers with its stored value, which may be null. Computed nodes are called
    /// only once every operand is present, so they may treat the list as complete.
    /// </para>
    /// </remarks>
    /// <param name="operands">The children's values, in <see cref="Children"/> order.</param>
    Measurand? ComputeFrom(IReadOnlyList<Measurand> operands);
}

/// <summary>
/// An <see cref="IExpression"/> that computes its value from child nodes and therefore needs an
/// <see cref="ErrorPropagation"/> policy for combining their uncertainties.
/// </summary>
public interface IComputedExpression : IExpression
{
    /// <summary>
    /// Whether child errors are treated as correlated or uncorrelated when their uncertainties are combined
    /// into this node's value.
    /// </summary>
    ErrorPropagationMethod ErrorPropagation { get; set; }
}

/// <summary>
/// An <see cref="IExpression"/> whose value is set directly rather than computed — a mutable leaf.
/// </summary>
public interface IDirectExpression : IExpression
{
    /// <summary>
    /// The leaf's stored value, settable. Assigning a <see cref="Measurand"/> whose dimensionality does not
    /// match this node's throws <c>IncompatibleDimensionsException</c>; assigning <see langword="null"/> makes
    /// the leaf unbound again.
    /// </summary>
    /// <remarks>
    /// A genuine property, unlike <see cref="IExpression.CalculateValueIfDetermined"/>: there is nothing beneath
    /// a leaf to walk, so reading it really is field access. The two used to share a name, which forced this one
    /// to shadow the other with <c>new</c> and hid the difference in cost between them.
    /// </remarks>
    Measurand? Value { get; set; }
}
