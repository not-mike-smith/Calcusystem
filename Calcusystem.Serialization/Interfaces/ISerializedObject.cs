namespace Calcusystem.Serialization.Interfaces;

/// <summary>
/// Common shape of a serialized DTO: a stable <see cref="Id"/> for rebuilding the reference graph and a
/// <see cref="Type"/> discriminator naming the concrete domain type it maps to.
/// </summary>
/// <remarks>
/// The flattened object graph references its nodes by <see cref="Id"/> rather than by nesting, so ids must
/// survive a round-trip. <see cref="Type"/> is the coupling point between the two mappers and the on-disk
/// format — written as the domain type's name and matched by name on the way back in.
/// </remarks>
public interface ISerializedObject
{
    /// <summary>Stable identity of the node, used to resolve references when rebuilding the graph.</summary>
    public string Id { get; }

    /// <summary>
    /// Discriminator naming the concrete domain type this DTO represents (e.g. "ProductExpression"), used to
    /// pick the right mapping on deserialization.
    /// </summary>
    public string Type { get; }
}
