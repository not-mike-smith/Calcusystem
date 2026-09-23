using Calcusystem.Measurement.Snapshots;

namespace Calcusystem.DimensionedExpression.Snapshots;

/// <summary>
/// The complete stored state of a <see cref="Expressions.Variable"/>.
/// </summary>
/// <remarks>
/// A variable is the one expression that rebuilds from its own state alone — it has no children to resolve — so
/// it uses the self-contained seam rather than the node one. Its dimensionality is stored independently of its
/// value because a variable is dimensioned from the moment it is declared, whether or not it is yet bound.
/// </remarks>
/// <param name="Id">Stable identity.</param>
/// <param name="Symbol">The variable's display symbol.</param>
/// <param name="Dimensionality">Its physical dimension, known even while unset.</param>
/// <param name="Value">Its value and uncertainty, or null while unset.</param>
/// <param name="Provenance">Where the value came from, or null when untracked.</param>
public readonly record struct VariableSnapshot(
    string Id,
    string Symbol,
    DimensionalitySnapshot Dimensionality,
    MeasurandSnapshot? Value,
    ProvenanceSnapshot? Provenance);
