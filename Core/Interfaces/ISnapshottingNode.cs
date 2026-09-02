namespace Calcusystem.Core.Interfaces;

/// <summary>
/// A node in an object graph that can hand out its state and be rebuilt from it, where the state refers to
/// neighbouring nodes <i>by id</i> rather than containing them.
/// </summary>
/// <remarks>
/// <para>
/// The counterpart to <see cref="ISnapshotting{TSelf,TSnapshot}"/> for types that cannot be rebuilt from their own
/// state alone. A graph is not a tree: one node can be shared by several parents, so nesting children inside a
/// parent's state would duplicate the shared ones and could not express the sharing at all. Referring to them by
/// id keeps the state flat and the graph intact — at the cost of needing something that can turn an id back into
/// a node, which is what <paramref name="resolve"/> supplies.
/// </para>
/// <para>
/// Supplying that resolver — and deciding the order in which nodes are rebuilt so their dependencies exist first
/// — is a persistence strategy, not domain knowledge. It belongs to the caller. This interface only says what a
/// node's state is and how to reconstitute one node given a way to look up its neighbours.
/// </para>
/// <para>
/// Neighbours need not share a type: <see cref="INodeResolver"/> is queried per reference, so a node that refers
/// to several different kinds of node is no harder to express than one that refers to a single kind.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The implementing type.</typeparam>
/// <typeparam name="TSnapshot">The state record describing this node, referring to neighbours by id.</typeparam>
public interface ISnapshottingNode<TSelf, TSnapshot> where TSelf : ISnapshottingNode<TSelf, TSnapshot>
{
    /// <summary>Returns the complete state defining this node, referring to its neighbours by id.</summary>
    TSnapshot GetSnapshot();

    /// <summary>
    /// Rebuilds a node from previously captured state. Not part of the normal construction API.
    /// </summary>
    /// <param name="state">The captured state.</param>
    /// <param name="resolve">
    /// Looks up the nodes this state references. The caller is responsible for rebuilding in an order that makes
    /// every referenced node available before it is asked for.
    /// </param>
    static abstract TSelf FromSnapshot(TSnapshot state, INodeResolver resolve);
}
