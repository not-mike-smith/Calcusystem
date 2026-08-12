using Calcusystem.Core;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Interfaces;

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
    /// <c>CalculateValueIfDetermined()</c> is this applied to children that computed themselves recursively; an
    /// evaluator is the same function applied to operands it computed in dependency order and kept. A node owns
    /// how values combine, and a caller owns the order they are produced in and whether any are worth keeping.
    /// </para>
    /// </remarks>
    /// <param name="known">Values already established, by node. Missing entries mean not yet computed.</param>
    /// <param name="propagator">
    /// How uncertainties are combined, or null for the conservative Gaussian default. A different axis from a
    /// computed node's <c>ErrorPropagation</c>: that says whether <i>these</i> operands are correlated, which is
    /// a statement about the model, while this is the numerical method and belongs to the calculation. Both are
    /// passed on together, so supplying one never discards the other.
    /// </param>
    Measurand? ComputeFrom(
        IReadOnlyDictionary<IExpression, Measurand> known,
        IErrorPropagator? propagator = null);
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
