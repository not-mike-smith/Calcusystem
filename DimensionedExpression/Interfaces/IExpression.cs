using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Interfaces;

/// <summary>
/// A node in a dimensioned expression tree — a leaf variable or a computed combination of other nodes.
/// A node's <see cref="Dimensionality"/> is always known (structural), but its value is produced
/// only once every leaf it depends on has been given a value.
/// </summary>
/// <remarks>
/// Implementations compute the value lazily on each call from their current children — there is no
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
    /// Whether every leaf this node depends on has a value, so <see cref="ComputeIfFullyDescribed"/> is non-null. Equivalent to
    /// <c>DegreesOfFreedom() == 0</c>.
    /// </summary>
    bool IsFullyDescribed { get; }

    /// <summary>
    /// The physical dimension of this node, known structurally and always available — even before any values are
    /// supplied.
    /// </summary>
    Dimensionality Dimensionality { get; }

    /// <summary>
    /// The nodes this one is computed from, in operand order; empty for a leaf. The single accessor every graph
    /// walk goes through — free-variable collection, dependency ordering, and incidence are all one traversal
    /// over this rather than a switch over node types.
    /// </summary>
    /// <remarks>
    /// A node may appear as a child of more than one parent: the graph is a DAG, not a tree, and shared
    /// sub-expressions are the point of referencing neighbours by id. Any walk must therefore deduplicate by
    /// <see cref="IIdentified.Id"/> — see <c>ExpressionGraph</c>, which does.
    /// </remarks>
    IEnumerable<IExpression> Children { get; }

    /// <summary>
    /// This node's value, looked up from <paramref name="known"/> — the node's own arithmetic and uncertainty
    /// propagation, with the walk that produced its operands factored out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Look up yourself and your own children, nothing else.</b> A composite reads its children's entries; a
    /// leaf reads its own, falling back to its stored value when absent — which is what makes an override a
    /// leaf's own business rather than something every caller has to special-case.
    /// </para>
    /// <para>
    /// Keyed rather than positional because position is a contract a caller can silently get wrong: handed a
    /// list, a quotient cannot tell numerator from denominator except by trusting the order, and computing
    /// <c>d/n</c> is not an error anything would catch. Looking children up by identity removes the question,
    /// and a child referenced twice needs only one entry.
    /// </para>
    /// <para>
    /// <c>ComputeIfFullyDescribed()</c> is this applied to children that computed themselves recursively; an
    /// evaluator is the same function applied to operands it computed in dependency order and kept. A node owns
    /// how values combine, and a caller owns the order they are produced in and whether any are worth keeping.
    /// </para>
    /// </remarks>
    /// <param name="known">Values already established, by node. Missing entries mean not yet computed.</param>
    /// <param name="propagator">
    /// How uncertainties are combined, or null for the conservative Gaussian default. A different axis from a
    /// computed node's <c>UncertaintyCorrelation</c>: that says whether <i>these</i> operands are correlated, which is
    /// a statement about the model, while this is the numerical method and belongs to the calculation. Both are
    /// passed on together, so supplying one never discards the other.
    /// </param>
    Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IUncertaintyPropagator? propagator = null);

    /// <summary>
    /// Computes this node's value with propagated uncertainty, or returns <see langword="null"/> if any leaf it
    /// depends on is still unset.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Named for what it costs.</b> This walks the whole graph beneath the node on every call and caches
    /// nothing, so a sub-expression shared by three parents is computed three times. It is a method, not a
    /// property, because a property invites callers to treat it as field access and call it in a loop.
    /// </para>
    /// <para>
    /// Nothing is memoised on purpose: a node has no way to learn that a leaf beneath it was reassigned, so a
    /// cached answer would go stale silently. Caching belongs to a caller that knows over what scope the graph is
    /// unchanged — <c>Calcusystem.Analysis</c>'s <c>system.Calculate()</c> computes each node once per run and
    /// reports what is missing besides. Prefer it for anything beyond a single node.
    /// </para>
    /// </remarks>
    /// <param name="overrides">
    /// Values supplied for this computation only, taking precedence over a variable's own — the same mechanism
    /// <c>Calculate</c> offers, for a caller working on one sub-expression rather than a whole system.
    /// </param>
    /// <param name="propagator">How uncertainties are combined, or null for the conservative Gaussian default.</param>
    /// <exception cref="Exceptions.CyclicExpressionGraphException">The graph beneath this node has a cycle.</exception>
    Measurand? ComputeIfFullyDescribed(
        IReadOnlyDictionary<Variable, Measurand>? overrides = null,
        IUncertaintyPropagator? propagator = null);

    /// <summary>
    /// This node and every node reachable from it, each yielded exactly once however many parents reference it.
    /// Order is unspecified.
    /// </summary>
    IEnumerable<IExpression> SelfAndDescendants();

    /// <summary>
    /// The distinct unset leaf variables reachable from this node — the values that must be supplied before it
    /// can produce one, and the unknowns it contributes to a system's degrees of freedom.
    /// </summary>
    /// <remarks>
    /// Only a <see cref="Expressions.Variable"/> can be free: it is the sole node whose value is assigned rather
    /// than computed, so it is the only thing a solver could be asked to determine. A computed node with unset
    /// leaves beneath it is not itself an unknown — it is the path by which those leaves are reached.
    /// </remarks>
    IEnumerable<Variable> UnsetVariables();

    /// <summary>
    /// This node and everything reachable from it, each once, children before parents — the order values can be
    /// computed in without ever needing one that has not been produced yet.
    /// </summary>
    /// <exception cref="Exceptions.CyclicExpressionGraphException">The graph beneath this node has a cycle.</exception>
    IReadOnlyList<IExpression> InDependencyOrder();
}

/// <summary>
/// An <see cref="IExpression"/> that computes its value from child nodes and therefore needs an
/// <see cref="UncertaintyCorrelation"/> policy for combining their uncertainties.
/// </summary>
public interface IComputedExpression : IExpression
{
    /// <summary>
    /// Whether this node's children are treated as having correlated or uncorrelated errors when their
    /// uncertainties are combined into its value.
    /// </summary>
    /// <remarks>
    /// Part of the model: it records something known about where the children's values came from. Distinct from
    /// the <see cref="IUncertaintyPropagator"/> a calculation supplies, which is the numerical method for combining
    /// uncertainties — see the remarks on <see cref="IExpression.ComputeFrom"/>, which passes both.
    /// </remarks>
    UncertaintyCorrelation UncertaintyCorrelation { get; set; }
}

/// <summary>
/// An <see cref="IExpression"/> whose value is set directly rather than computed — a mutable leaf.
/// </summary>
public interface IDirectExpression : IExpression
{
    /// <summary>
    /// The leaf's stored value, settable. Assigning a <see cref="Measurand"/> whose dimensionality does not
    /// match this node's throws <c>IncompatibleDimensionsException</c>; assigning <see langword="null"/> makes
    /// the leaf unset again.
    /// </summary>
    /// <remarks>
    /// A genuine property, unlike <see cref="IExpression.ComputeIfFullyDescribed"/>: there is nothing beneath
    /// a leaf to walk, so reading it really is field access. The two used to share a name, which forced this one
    /// to shadow the other with <c>new</c> and hid the difference in cost between them.
    /// </remarks>
    Measurand? Value { get; set; }
}
