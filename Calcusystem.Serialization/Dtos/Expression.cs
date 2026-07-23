using Calcusystem.Serialization.Interfaces;
using Measurement;
using Measurement.Models;

namespace Calcusystem.Serialization.Dtos;

public abstract class ExpressionBase : ISerializedObject
{
    public required string Type { get; init; }
    public required string Id { get; init; }
}

public abstract class Uncertainty
{
    public required string Type { get; init; }
}

public class SymmetricUncertainty : Uncertainty
{
    public required double? RelativeError { get; init; }
}

public class AsymmetricUncertainty : Uncertainty
{
    public required double? UpperRelativeError { get; init; }
    public required double? LowerRelativeError { get; init; }
}

public class SingleVariable : ExpressionBase
{
    public required string Symbol { get; init; }
    public required Dimensionality Dimensionality { get; init; }
    public required double? KmsValue { get; set; }
    public required Uncertainty? Uncertainty { get; init; }
}

public class SingleDerivedVariable : ExpressionBase
{
    public required string InnerId { get; init; }
}

public class PairDerivedVariable : ExpressionBase
{
    public required string InnerId1 { get; init; }
    public required string InnerId2 { get; init; }
}

public class ListDerivedVariable : ExpressionBase
{
    public required List<string> InnerIds { get; init; }
    public required ErrorPropagationMethod  ErrorPropagation { get; init; }
}
