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
public interface IExpression : IIdentified
{
    /// <summary>
    /// Whether this node's value can be set directly. True for leaf variables (<see cref="IDirectExpression"/>);
    /// false for computed nodes, whose value derives from their children.
    /// </summary>
    bool IsDirectlyMutable { get; }

    /// <summary>
    /// Whether every leaf this node depends on has a value, so <see cref="ComputeIfFullyDescribed"/> is
    /// non-null. Equivalent to <see cref="UnsetVariables"/> being empty.
    /// </summary>
    bool IsFullyDescribed { get; }

    /// <summary>
    /// The physical dimension of this node, known structurally and always available — even before any values are
    /// supplied.
    /// </summary>
    Dimensionality Dimensionality { get; }

    /// <summary>
    /// The nodes this one is computed from, in operand order; empty for a leaf. The single accessor every
    /// graph walk goes through.
    /// </summary>
    IEnumerable<IExpression> Children { get; }

    /// <summary>
    /// This node's value, looked up from <paramref name="known"/> — the node's own arithmetic and uncertainty
    /// propagation, with the walk that produced its operands factored out.
    /// </summary>
    /// <remarks>
    /// The rule for an implementor: <b>look up yourself and your own children, nothing else.</b>
    /// </remarks>
    /// <param name="known">Values already established, by node. Missing entries mean not yet computed.</param>
    /// <param name="propagator">How uncertainties are combined, or null for the conservative Gaussian default.</param>
    Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IUncertaintyPropagator? propagator = null);

    /// <summary>
    /// Computes this node's value with propagated uncertainty, or returns <see langword="null"/> if any leaf it
    /// depends on is still unset.
    /// </summary>
    /// <remarks>
    /// Walks the whole graph beneath the node on every call and caches nothing. Prefer
    /// <c>Calcusystem.Analysis</c>'s <c>system.Calculate()</c> for anything beyond a single node.
    /// </remarks>
    /// <param name="overrides">
    /// Values supplied for this computation only, taking precedence over a variable's own.
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
    /// Only a <see cref="Expressions.Variable"/> is ever unset: a computed node with unset leaves beneath it is
    /// the path by which they are reached, not an unknown of its own.
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
    /// Whether this node's children are treated as having correlated or uncorrelated uncertainties when their
    /// uncertainties are combined into its value.
    /// </summary>
    /// <remarks>
    /// Part of the model, not a numerical method — that is the <see cref="IUncertaintyPropagator"/> a
    /// calculation supplies. Both are passed on together.
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
    /// a leaf to walk, so reading it really is field access. The names differ to keep that cost difference
    /// visible.
    /// </remarks>
    Measurand? Value { get; set; }
}
