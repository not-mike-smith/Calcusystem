namespace DimensionedExpression.Interfaces;

/// <summary>
/// Describes where a leaf variable's value came from — an audit annotation, not a behavioral trait. Provenance
/// is attached to a <c>Variable</c> by composition (see its <c>Provenance</c> property); it does not change how
/// the variable evaluates. Concrete kinds (measured reading, reference constant, design parameter, model
/// parameter, …) are an open, extensible set created exclusively through <c>ProvenanceFactory</c>.
/// </summary>
/// <remarks>
/// Provenance is self-serializing: <see cref="Serialize"/> produces a self-describing payload and
/// <c>ProvenanceFactory.Deserialize</c> reconstructs it. This keeps the provenance taxonomy extensible without
/// the closed, switch-based mappers in <c>Calcusystem.Serialization</c> needing to know every kind.
/// </remarks>
public interface IProvenance
{
    /// <summary>A one-line, human-readable description suitable for display in a UI.</summary>
    string Summary();

    /// <summary>
    /// Serializes this provenance to a self-describing payload string (it carries the discriminator that
    /// <c>ProvenanceFactory.Deserialize</c> uses to reconstruct the correct kind).
    /// </summary>
    string Serialize();
}
