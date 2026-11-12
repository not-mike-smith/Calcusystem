using Calcusystem.Measurement.Snapshots;

namespace Calcusystem.DimensionedExpression.Snapshots;

/// <summary>
/// The complete snapshot of a <see cref="Expressions.Variable"/>.
/// </summary>
/// <remarks>
/// A variable rebuilds from its own snapshot alone — it has no children to resolve — so it uses the
/// self-contained seam rather than the node one. Its dimensionality is stored independently of its
/// value because a variable is dimensioned from the moment it is declared, whether or not it is yet bound.
/// </remarks>
/// <param name="Id">Stable identity.</param>
/// <param name="Name">The variable's name.</param>
/// <param name="Dimensionality">Its physical dimension, known even while unset.</param>
/// <param name="Value">Its value and uncertainty, or null while unset.</param>
/// <param name="Provenance">Where the value came from, or null when untracked.</param>
public readonly record struct VariableSnapshot(
    string Id,
    string Name,
    DimensionalitySnapshot Dimensionality,
    MeasurandSnapshot? Value,
    ProvenanceSnapshot? Provenance);
