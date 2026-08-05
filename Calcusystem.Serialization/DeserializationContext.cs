using Calcusystem.Core;
using Calcusystem.Serialization.Exceptions;
using Calcusystem.Serialization.Interfaces;
using DimensionedExpression.Interfaces;

namespace Calcusystem.Serialization;

/// <summary>
/// The id-resolution table threaded through one deserialization run. Accumulates state, so use a fresh instance
/// per run.
/// </summary>
/// <remarks>
/// Also the <see cref="INodeResolver"/> handed to domain types as they rebuild. Holding both roles in one type
/// keeps the ordering strategy and the lookup it feeds in the same place — the domain only ever sees the
/// resolver face.
/// </remarks>
public class DeserializationContext : INodeResolver
{
    private readonly Dictionary<string, object> _nodesById = new();

    /// <summary>Every expression loaded so far, by id.</summary>
    public IReadOnlyDictionary<string, IExpression> ExpressionsById =>
        _nodesById.Values.OfType<IExpression>().ToDictionary(x => x.Id);

    /// <summary>Records a rebuilt expression so later nodes can reference it.</summary>
    public void AddLoadedExpression(IExpression x) => _nodesById.Add(x.Id, x);

    /// <summary>Records a rebuilt operator so the containing system can reference it.</summary>
    public void AddLoadedOperator(IBinaryOperator x) => _nodesById.Add(x.Id, x);

    /// <summary>Whether a node with this id has been loaded yet.</summary>
    public bool Contains(string id) => _nodesById.ContainsKey(id);

    /// <summary>
    /// The DTO currently being rebuilt, reported by <see cref="ReferencedNodeNotFoundException"/> so a failure
    /// names what referenced the missing id rather than just the id.
    /// </summary>
    internal ISerializedObject? ReferencingDto { get; set; }

    /// <inheritdoc/>
    /// <exception cref="ReferencedNodeNotFoundException">
    /// No node has that id, or it is not a <typeparamref name="TNode"/>.
    /// </exception>
    public TNode Resolve<TNode>(string id) where TNode : class
    {
        if (! _nodesById.TryGetValue(id, out var node))
            throw new ReferencedNodeNotFoundException(id, ReferencingDto!);

        return node as TNode
               ?? throw new ReferencedNodeNotFoundException(
                   id,
                   ReferencingDto!,
                   $"Node '{id}' is a {node.GetType().Name}, not a {typeof(TNode).Name}.");
    }
}
