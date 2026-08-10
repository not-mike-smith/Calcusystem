using Calcusystem.Serialization.Interfaces;

namespace Calcusystem.Serialization.Dtos;

public class ExpressionSystem : ISerializedObject
{
    public required string Id { get; set; }
    public required string Type { get; init; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    // These need an `init` setter, not merely a getter over a pre-built list. A get-only collection property is
    // written correctly by a serializer but cannot be restored by one — System.Text.Json skips it rather than
    // adding to the existing instance — so every list came back empty with no error raised. Covered by
    // JsonRoundTripTests.
    public List<SingleVariable> DirectExpressions { get; init; } = new();
    public List<SingleDerivedVariable> SingleDerivedVariables { get; init; } = new();
    public List<ListDerivedVariable> ListDerivedVariables { get; init; } = new();
    public List<PairDerivedVariable> PairDerivedVariables { get; init; } = new();
    public List<BinaryOperator> Relationships { get; init; } = new();
}
