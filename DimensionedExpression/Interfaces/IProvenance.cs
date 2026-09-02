using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;

namespace Calcusystem.DimensionedExpression.Interfaces;

/// <summary>
/// Describes where a leaf variable's (or an operator's) value came from — an audit annotation, not a behavioral
/// trait. Provenance is attached by composition (see <c>Variable.Provenance</c> / <c>IBinaryOperator.Provenance</c>)
/// and never changes how anything evaluates. Concrete kinds (measured reading, reference constant, design
/// parameter, model parameter, …) are created exclusively through <c>ProvenanceFactory</c>.
/// </summary>
/// <remarks>
/// Provenance carries an <see cref="IIdentified.Id"/> so it round-trips through <c>Calcusystem.Serialization</c> like any
/// other serialized object, even though it is always owned inline by a single node rather than referenced by id.
/// Serialization itself lives in that assembly, not here.
/// </remarks>
public interface IProvenance : IIdentified
{

    /// <summary>A one-line, human-readable description suitable for display in a UI.</summary>
    string Summary();

    /// <summary>
    /// Returns the complete stored state of this provenance — its kind and that kind's audit metadata — for
    /// rebuilding via <c>ProvenanceFactory.FromSnapshot</c>.
    /// </summary>
    /// <remarks>
    /// The persistence seam. Implementations provide this <i>explicitly</i>, so the metadata stays off their own
    /// public surface: a consumer holding a <c>MeasuredProvenance</c> sees <see cref="Summary"/> and
    /// <see cref="IIdentified.Id"/>, not the raw fields. Reading them is a persistence concern, and this is its one door.
    /// </remarks>
    ProvenanceSnapshot GetSnapshot();
}
