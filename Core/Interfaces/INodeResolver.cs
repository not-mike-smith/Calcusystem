namespace Calcusystem.Core.Interfaces;

/// <summary>
/// Turns an id reference back into the node it names, while a graph is being rebuilt from a snapshot.
/// </summary>
/// <remarks>
/// <para>
/// The type argument is a claim about what the referenced id names, checked when it is resolved. That check is
/// necessarily a runtime one.
/// </para>
/// <para>
/// Implementations throw when an id cannot be resolved, or names a node of a different type. Callers rebuilding
/// a graph are expected to order the work so that every referenced node already exists — a failure here means
/// the source data is not internally consistent.
/// </para>
/// </remarks>
public interface INodeResolver
{
    /// <summary>Returns the node with the given id.</summary>
    /// <typeparam name="TNode">The type the referenced node is expected to have.</typeparam>
    /// <param name="id">Identity of the referenced node.</param>
    /// <exception cref="Exception">
    /// Implementation-defined, when no node has that id or it is not a <typeparamref name="TNode"/>.
    /// </exception>
    TNode Resolve<TNode>(string id) where TNode : class, IIdentified;
}
