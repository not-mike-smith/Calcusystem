using Calcusystem.Serialization.Interfaces;

namespace Calcusystem.Serialization.Dtos;

public class BinaryOperator : ISerializedObject
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string? Name { get; init; }
    public required string? Description { get; init; }
    public required string LhsId { get; init; }
    public required string RhsId { get; init; }
}
