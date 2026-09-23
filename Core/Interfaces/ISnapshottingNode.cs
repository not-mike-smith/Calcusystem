namespace Calcusystem.Core.Interfaces;

/// <summary>
/// A node in an object graph that can hand out its snapshot and be rebuilt from it, where the snapshot refers to
/// neighbouring nodes <i>by id</i> rather than containing them.
/// </summary>
/// <remarks>
/// <para>
/// The counterpart to <see cref="ISnapshotting{TSelf,TSnapshot}"/> for types that cannot be rebuilt from their
/// own snapshot alone. A graph is not a tree, so nesting children inside a parent's snapshot would duplicate
/// the shared ones; naming them by id keeps each snapshot flat and the graph intact.
/// </para>
/// <para>
/// Supplying the <see cref="INodeResolver"/>, and rebuilding in an order that makes each referenced node
/// available before it is asked for, is a persistence strategy rather than domain knowledge, so it belongs to
/// the caller. This interface only says what a node's snapshot is and how to rebuild one node given a way to
/// look up its neighbours.
/// </para>
/// <para>
/// Neighbours need not share a type: <see cref="INodeResolver"/> is queried per reference, so a node referring
/// to several kinds of node is no harder to express than one referring to a single kind.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The implementing type.</typeparam>
/// <typeparam name="TSnapshot">The snapshot describing this node, referring to neighbours by id.</typeparam>
public interface ISnapshottingNode<TSelf, TSnapshot> where TSelf : ISnapshottingNode<TSelf, TSnapshot>
{
    /// <summary>Returns the complete snapshot defining this node, referring to its neighbours by id.</summary>
    TSnapshot GetSnapshot();

    /// <summary>
    /// Rebuilds a node from a previously captured snapshot. Not part of the normal construction API.
    /// </summary>
    /// <param name="snapshot">The captured snapshot.</param>
    /// <param name="resolve">
    /// Looks up the nodes this snapshot references. The caller is responsible for rebuilding in an order that makes
    /// every referenced node available before it is asked for.
    /// </param>
    static abstract TSelf FromSnapshot(TSnapshot snapshot, INodeResolver resolve);
}
