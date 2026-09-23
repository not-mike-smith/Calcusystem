using Calcusystem.Serialization.Interfaces;

namespace Calcusystem.Serialization.Exceptions;

/// <summary>
/// A serialized object referenced another node by id, and no node with that id was present in the payload.
/// </summary>
/// <remarks>
/// <para>
/// Raised while resolving id references during deserialization — for any referenced node, not only expressions:
/// operators, sub-systems, and anything else the graph refers to by id.
/// </para>
/// <para>
/// This means the payload is not internally consistent, so deserialization stops rather than loading a partial
/// graph.
/// </para>
/// </remarks>
public class ReferencedNodeNotFoundException : Exception
{
    /// <summary>Id that could not be resolved.</summary>
    public readonly string IdOfMissingNode;

    /// <summary>The serialized object that referenced it.</summary>
    public readonly ISerializedObject ReferencingDto;

    public ReferencedNodeNotFoundException(
        string idOfMissingNode,
        ISerializedObject referencingDto,
        string? message = null,
        Exception? innerException = null) : base(message, innerException)
    {
        IdOfMissingNode = idOfMissingNode;
        ReferencingDto = referencingDto;
    }
}
