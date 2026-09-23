namespace Calcusystem.Core.Interfaces;

/// <summary>
/// A node in an object graph that can hand out its snapshot and be rebuilt from it, where the snapshot refers to
/// neighbouring nodes <i>by id</i> rather than containing them.
/// </summary>
/// <remarks>
/// The counterpart to <see cref="ISnapshotting{TSelf,TSnapshot}"/> for types that cannot be rebuilt from their own
/// snapshot alone.
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
