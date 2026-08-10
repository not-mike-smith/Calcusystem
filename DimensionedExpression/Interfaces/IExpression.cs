using Calcusystem.Core;
using Measurement;

namespace DimensionedExpression.Interfaces;

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
    /// The computed value with propagated uncertainty, or <see langword="null"/> while
    /// <see cref="IsFullyDescribed"/> is false. Recomputed from the current children on each access.
    /// </summary>
    Measurand? Value { get; }

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
/// An <see cref="IExpression"/> whose value is set directly rather than computed — a mutable leaf. Re-declares
/// <see cref="Value"/> with a setter.
/// </summary>
public interface IDirectExpression : IExpression
{
    /// <summary>
    /// The leaf's value, settable. Assigning a <see cref="Measurand"/> whose dimensionality does not match this
    /// node's throws <c>IncompatibleDimensionsException</c>; assigning <see langword="null"/> makes the leaf
    /// unbound again.
    /// </summary>
    new Measurand? Value { get; set; }
}
