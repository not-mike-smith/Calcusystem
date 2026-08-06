namespace Calcusystem.Core;

/// <summary>
/// Turns an id reference back into the node it names, while a graph is being rebuilt from state.
/// </summary>
/// <remarks>
/// <para>
/// A generic method rather than a typed delegate, because a node's neighbours are not necessarily all the same
/// type — an <c>ExpressionSystem</c> refers to both expressions and operators by id, and a composed system would
/// refer to sub-systems as well. A <c>Func&lt;string, TNode&gt;</c> can express only the homogeneous case.
/// </para>
/// <para>
/// The type argument is a claim about what the referenced id names, checked when it is resolved. That check is
/// necessarily a runtime one: an id reference carries no type information, so no signature could have proved it
/// statically.
/// </para>
/// <para>
/// Implementations throw when an id cannot be resolved, or names a node of a different type. Callers rebuilding
/// a graph are expected to order the work so that every referenced node already exists — a failure here means
/// the source data is not internally consistent, which is not something a domain type should be asked to paper
/// over.
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
