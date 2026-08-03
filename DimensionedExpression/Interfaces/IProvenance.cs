namespace DimensionedExpression.Interfaces;

/// <summary>
/// Describes where a leaf variable's (or an operator's) value came from — an audit annotation, not a behavioral
/// trait. Provenance is attached by composition (see <c>Variable.Provenance</c> / <c>IBinaryOperator.Provenance</c>)
/// and never changes how anything evaluates. Concrete kinds (measured reading, reference constant, design
/// parameter, model parameter, …) are created exclusively through <c>ProvenanceFactory</c>.
/// </summary>
/// <remarks>
/// Provenance carries an <see cref="Id"/> so it round-trips through <c>Calcusystem.Serialization</c> like any
/// other serialized object, even though it is always owned inline by a single node rather than referenced by id.
/// Serialization itself lives in that assembly, not here.
/// </remarks>
public interface IProvenance
{
    /// <summary>Stable identity, preserved across serialization.</summary>
    string Id { get; }

    /// <summary>A one-line, human-readable description suitable for display in a UI.</summary>
    string Summary();
}
