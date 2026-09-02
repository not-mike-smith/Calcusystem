using Calcusystem.Measurement.Enums;
using Calcusystem.Serialization.Interfaces;
using Calcusystem.Measurement.Snapshots;

namespace Calcusystem.Serialization.Dtos;

public abstract class ExpressionBase : ISerializedObject
{
    public required string Type { get; init; }
    public required string Id { get; init; }
}

/// <summary>
/// A serialized uncertainty: a <see cref="Type"/> discriminator plus the union of the shapes' fields, exactly as
/// <see cref="Provenance"/> handles its kinds. <see cref="Magnitude"/> is populated for the symmetric shape;
/// <see cref="UpperMagnitude"/>/<see cref="LowerMagnitude"/> for the asymmetric one.
/// </summary>
/// <remarks>
/// Flat and concrete rather than an abstract base with two subclasses, because a serializer cannot instantiate an
/// abstract type without provider-specific polymorphism configuration — and configuring that here would tie this
/// assembly to one serializer, which is precisely what it declines to do.
/// </remarks>
public class Uncertainty
{
    public required string Type { get; init; }
    public required bool IsStoredAsAbs { get; init; }
    public double? Magnitude { get; init; }
    public double? UpperMagnitude { get; init; }
    public double? LowerMagnitude { get; init; }
}

public class SingleVariable : ExpressionBase
{
    public required string Symbol { get; init; }

    /// <summary>
    /// The variable's dimension in its canonical encoded form (e.g. <c>"M1,L1,T-2"</c>); empty for a
    /// dimensionless variable. See <see cref="DimensionalitySnapshot"/>.
    /// </summary>
    /// <remarks>
    /// A string, not the <c>Dimensionality</c> struct. The struct's exponent map is private, so a serializer
    /// handed one writes <c>{}</c> and reads back a dimensionless value with no error — which is what this
    /// property used to do. If the fundamental-dimension symbols ever change, migrating previously persisted
    /// values is this layer's responsibility.
    /// </remarks>
    public required string Dimensionality { get; init; }
    public required double? KmsValue { get; set; }
    public required Uncertainty? Uncertainty { get; init; }
    public Provenance? Provenance { get; init; }
}

public class SingleDerivedVariable : ExpressionBase
{
    public required string InnerId { get; init; }
}

public class PairDerivedVariable : ExpressionBase
{
    public required string InnerId1 { get; init; }
    public required string InnerId2 { get; init; }

    /// <summary>
    /// How child uncertainties are combined. Not <c>required</c>: payloads written before this field existed
    /// simply lack it, and <see cref="UncertaintyCorrelation.Uncorrelated"/> — the default both here and on the
    /// expression itself — is what they meant.
    /// </summary>
    public UncertaintyCorrelation UncertaintyCorrelation { get; init; }
}

public class ListDerivedVariable : ExpressionBase
{
    public required List<string> InnerIds { get; init; }
    public required UncertaintyCorrelation UncertaintyCorrelation { get; init; }
}
