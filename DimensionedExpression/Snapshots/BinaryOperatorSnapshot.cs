using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression.Enums;

namespace Calcusystem.DimensionedExpression.Snapshots;

/// <summary>
/// The complete snapshot of any binary operator. Every operator has the same shape — two operand references
/// plus annotations — so one record with a <see cref="Type"/> discriminator covers every one.
/// </summary>
/// <param name="Type">Which operator this snapshot rebuilds into.</param>
/// <param name="Id">Stable identity.</param>
/// <param name="LhsId">Id of the left-hand expression.</param>
/// <param name="RhsId">Id of the right-hand expression.</param>
/// <param name="SolvingRole">
/// What this relationship does to the problem. Only the equality kind can store anything but
/// <see cref="Enums.SolvingRole.Requirement"/>; reconstruction ignores it for every other kind, which has no
/// way to represent one.
/// </param>
/// <param name="Agreement">
/// How strictly an equality reads "equal", and null for every other kind. Reconstruction refuses an equality
/// that has none rather than guessing a reading.
/// </param>
/// <param name="Rule">
/// The comparison a <see cref="BinaryOperatorType.SimpleComparison"/> asserts, and null for every other kind,
/// whose rules are fixed by their type.
/// </param>
/// <param name="Name">Optional human-readable name.</param>
/// <param name="Description">Optional human-readable description.</param>
/// <param name="Provenance">Where the relationship came from (e.g. a citation), or null when untracked.</param>
public readonly record struct BinaryOperatorSnapshot(
    BinaryOperatorType Type,
    string Id,
    string LhsId,
    string RhsId,
    SolvingRole SolvingRole,
    AgreementRule? Agreement,
    ComparisonRule? Rule,
    string? Name,
    string? Description,
    ProvenanceSnapshot? Provenance);
