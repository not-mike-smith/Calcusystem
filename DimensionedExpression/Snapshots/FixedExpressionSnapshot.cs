using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Snapshots;

namespace Calcusystem.DimensionedExpression.Snapshots;

/// <summary>
/// The complete snapshot of a fixed value: a <see cref="Expressions.Constant"/>, a
/// <see cref="Expressions.ReferenceValue"/> or a <see cref="Expressions.Scalar"/>.
/// </summary>
/// <param name="Type">Which of the three to rebuild.</param>
/// <param name="Id">Stable identity.</param>
/// <param name="Name">The name. Null for a scalar, and only for one.</param>
/// <param name="Value">Its value and uncertainty. The dimensionality is the value's.</param>
/// <param name="Provenance">Where the value came from, or null when untracked. Always null for a scalar.</param>
public readonly record struct FixedExpressionSnapshot(
    FixedExpressionType Type,
    string Id,
    string? Name,
    MeasurandSnapshot Value,
    ProvenanceSnapshot? Provenance);
