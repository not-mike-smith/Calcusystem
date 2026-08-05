namespace Calcusystem.Core;

/// <summary>
/// A node in an object graph that can hand out its state and be rebuilt from it, where the state refers to
/// neighbouring nodes <i>by id</i> rather than containing them.
/// </summary>
/// <remarks>
/// <para>
/// The counterpart to <see cref="IStateful{TSelf,TState}"/> for types that cannot be rebuilt from their own
/// state alone. A graph is not a tree: one node can be shared by several parents, so nesting children inside a
/// parent's state would duplicate the shared ones and could not express the sharing at all. Referring to them by
/// id keeps the state flat and the graph intact — at the cost of needing something that can turn an id back into
/// a node, which is what <c>resolve</c> supplies.
/// </para>
/// <para>
/// Supplying that resolver — and deciding the order in which nodes are rebuilt so their dependencies exist first
/// — is a persistence strategy, not domain knowledge. It belongs to the caller. This interface only says what a
/// node's state is and how to reconstitute one node given a way to look up its neighbours.
/// </para>
/// <para>
/// <typeparamref name="TNode"/> is a type parameter rather than a fixed graph type, so this interface stays
/// independent of any particular object model. Ids are <see langword="string"/> throughout Calcusystem, so the
/// key type is fixed rather than parameterized.
/// </para>
/// </remarks>
/// <typeparam name="TSelf">The implementing type.</typeparam>
/// <typeparam name="TState">The state record describing this node, referring to neighbours by id.</typeparam>
/// <typeparam name="TNode">The graph's common node type, which <c>resolve</c> returns.</typeparam>
public interface IStatefulNode<TSelf, TState, TNode> where TSelf : IStatefulNode<TSelf, TState, TNode>
{
    /// <summary>Returns the complete state defining this node, referring to its neighbours by id.</summary>
    TState GetState();

    /// <summary>
    /// Rebuilds a node from previously captured state. Not part of the normal construction API.
    /// </summary>
    /// <param name="state">The captured state.</param>
    /// <param name="resolve">
    /// Turns a referenced id into the node it names. The caller is responsible for rebuilding in an order that
    /// makes every referenced node available before it is asked for.
    /// </param>
    static abstract TSelf FromState(TState state, Func<string, TNode> resolve);
}
